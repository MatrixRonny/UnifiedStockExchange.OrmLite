//
// ServiceStack.OrmLite: Light-weight POCO ORM for .NET and Mono
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Converters;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ServiceStack.OrmLite
{
    public class ModelDefinition
    {
        #region Static create using cache

        private static Dictionary<string, ModelDefinition> typeModelDefinitionMap;

        static ModelDefinition()
        {
            typeModelDefinitionMap = new();
        }

        internal static bool CheckForIdField(IEnumerable<PropertyInfo> objProperties)
        {
            // Not using Linq.Where() and manually iterating through objProperties just to avoid dependencies on System.Xml??
            foreach (var objProperty in objProperties)
            {
                if (objProperty.Name != OrmLiteConfig.IdField) continue;
                return true;
            }
            return false;
        }

        internal static void ClearCache()
        {
            // Key is formed by concatenating $"{type.Name}-{alias}";
            typeModelDefinitionMap = new Dictionary<string, ModelDefinition>();
        }

        public static ModelDefinition CreateInstance(Type modelType, string tableName = null)
        {
            string cacheKey = tableName == null ? modelType.Name : $"{modelType.Name}-{tableName}";
            if (typeModelDefinitionMap.TryGetValue(cacheKey, out var modelDef))
                return modelDef;

            if (modelType.IsValueType || modelType == typeof(string))
                return null;

            var modelAliasAttr = modelType.FirstAttribute<AliasAttribute>();
            var schemaAttr = modelType.FirstAttribute<SchemaAttribute>();

            var preCreates = modelType.AllAttributes<PreCreateTableAttribute>();
            var postCreates = modelType.AllAttributes<PostCreateTableAttribute>();
            var preDrops = modelType.AllAttributes<PreDropTableAttribute>();
            var postDrops = modelType.AllAttributes<PostDropTableAttribute>();

            string JoinSql(List<string> statements)
            {
                if (statements.Count == 0)
                    return null;
                var sb = StringBuilderCache.Allocate();
                foreach (var sql in statements)
                {
                    if (sb.Length > 0)
                        sb.AppendLine(";");
                    sb.Append(sql);
                }
                var to = StringBuilderCache.ReturnAndFree(sb);
                return to;
            }

            modelDef = new ModelDefinition
            {
                ModelType = modelType,
                Name = modelType.Name,
                Alias = modelAliasAttr?.Name,
                Schema = schemaAttr?.Name,
                PreCreateTableSql = JoinSql(preCreates.Map(x => x.Sql)),
                PostCreateTableSql = JoinSql(postCreates.Map(x => x.Sql)),
                PreDropTableSql = JoinSql(preDrops.Map(x => x.Sql)),
                PostDropTableSql = JoinSql(postDrops.Map(x => x.Sql)),
            };

            modelDef.CompositeIndexes.AddRange(
                modelType.AllAttributes<CompositeIndexAttribute>().ToList());

            modelDef.UniqueConstraints.AddRange(
                modelType.AllAttributes<UniqueConstraintAttribute>().ToList());

            var objProperties = modelType.GetProperties(
                BindingFlags.Public | BindingFlags.Instance).ToList();

            var hasPkAttr = objProperties.Any(p => p.HasAttributeCached<PrimaryKeyAttribute>());

            var hasIdField = CheckForIdField(objProperties);

            var i = 0;
            var propertyInfoIdx = 0;
            foreach (var propertyInfo in objProperties)
            {
                if (propertyInfo.GetIndexParameters().Length > 0)
                    continue; //Is Indexer

                var sequenceAttr = propertyInfo.FirstAttribute<SequenceAttribute>();
                var computeAttr = propertyInfo.FirstAttribute<ComputeAttribute>();
                var computedAttr = propertyInfo.FirstAttribute<ComputedAttribute>();
                var persistedAttr = propertyInfo.FirstAttribute<PersistedAttribute>();
                var customSelectAttr = propertyInfo.FirstAttribute<CustomSelectAttribute>();
                var decimalAttribute = propertyInfo.FirstAttribute<DecimalLengthAttribute>();
                var belongToAttribute = propertyInfo.FirstAttribute<BelongToAttribute>();
                var referenceAttr = propertyInfo.FirstAttribute<ReferenceAttribute>();

                var isRowVersion = propertyInfo.Name == ModelDefinition.RowVersionName
                    && (propertyInfo.PropertyType == typeof(ulong) || propertyInfo.PropertyType == typeof(byte[]));

                var isNullableType = propertyInfo.PropertyType.IsNullableType();

                var isNullable = (!propertyInfo.PropertyType.IsValueType
                                   && !propertyInfo.HasAttributeNamed(nameof(RequiredAttribute)))
                                   || isNullableType;

                var propertyType = isNullableType
                    ? Nullable.GetUnderlyingType(propertyInfo.PropertyType)
                    : propertyInfo.PropertyType;


                Type treatAsType = null;

                if (propertyType.IsEnum)
                {
                    var enumKind = EnumConverter.GetEnumKind(propertyType);
                    if (enumKind == EnumKind.Int)
                        treatAsType = Enum.GetUnderlyingType(propertyType);
                    else if (enumKind == EnumKind.Char)
                        treatAsType = typeof(char);
                }

                var isReference = referenceAttr != null && propertyType.IsClass;
                var isIgnored = propertyInfo.HasAttributeCached<IgnoreAttribute>() || isReference;

                var isFirst = !isIgnored && i++ == 0;

                var isAutoId = propertyInfo.HasAttributeCached<AutoIdAttribute>();

                var isPrimaryKey = (!hasPkAttr && (propertyInfo.Name == OrmLiteConfig.IdField || (!hasIdField && isFirst)))
                   || propertyInfo.HasAttributeNamed(nameof(PrimaryKeyAttribute))
                   || isAutoId;

                var isAutoIncrement = isPrimaryKey && propertyInfo.HasAttributeCached<AutoIncrementAttribute>();

                if (isAutoIncrement && propertyInfo.PropertyType == typeof(Guid))
                    throw new NotSupportedException($"[AutoIncrement] is only valid for integer properties for {modelType.Name}.{propertyInfo.Name} Guid property use [AutoId] instead");

                if (isAutoId && (propertyInfo.PropertyType == typeof(int) || propertyInfo.PropertyType == typeof(long)))
                    throw new NotSupportedException($"[AutoId] is only valid for Guid properties for {modelType.Name}.{propertyInfo.Name} integer property use [AutoIncrement] instead");

                var aliasAttr = propertyInfo.FirstAttribute<AliasAttribute>();

                var indexAttr = propertyInfo.FirstAttribute<IndexAttribute>();
                var isIndex = indexAttr != null;
                var isUnique = isIndex && indexAttr.Unique;

                var stringLengthAttr = propertyInfo.CalculateStringLength(decimalAttribute);

                var defaultValueAttr = propertyInfo.FirstAttribute<DefaultAttribute>();

                var referencesAttr = propertyInfo.FirstAttribute<ReferencesAttribute>();
                var fkAttr = propertyInfo.FirstAttribute<ForeignKeyAttribute>();
                var customFieldAttr = propertyInfo.FirstAttribute<CustomFieldAttribute>();
                var chkConstraintAttr = propertyInfo.FirstAttribute<CheckConstraintAttribute>();

                var order = propertyInfoIdx++;
                if (customFieldAttr != null) order = customFieldAttr.Order;

                var fieldDefinition = new FieldDefinition
                {
                    Name = propertyInfo.Name,
                    Alias = aliasAttr?.Name,
                    FieldType = propertyType,
                    FieldTypeDefaultValue = isNullable ? null : propertyType.GetDefaultValue(),
                    TreatAsType = treatAsType,
                    PropertyInfo = propertyInfo,
                    IsNullable = isNullable,
                    IsPrimaryKey = isPrimaryKey,
                    AutoIncrement = isPrimaryKey && isAutoIncrement,
                    AutoId = isAutoId,
                    IsIndexed = !isPrimaryKey && isIndex,
                    IsUniqueIndex = isUnique,
                    IsClustered = indexAttr?.Clustered == true,
                    IsNonClustered = indexAttr?.NonClustered == true,
                    IndexName = indexAttr?.Name,
                    IsRowVersion = isRowVersion,
                    IgnoreOnInsert = propertyInfo.HasAttributeCached<IgnoreOnInsertAttribute>(),
                    IgnoreOnUpdate = propertyInfo.HasAttributeCached<IgnoreOnUpdateAttribute>(),
                    ReturnOnInsert = propertyInfo.HasAttributeCached<ReturnOnInsertAttribute>(),
                    FieldLength = stringLengthAttr?.MaximumLength,
                    DefaultValue = defaultValueAttr?.DefaultValue,
                    CheckConstraint = chkConstraintAttr?.Constraint,
                    IsUniqueConstraint = propertyInfo.HasAttributeCached<UniqueAttribute>(),
                    ForeignKey = fkAttr == null
                        ? referencesAttr != null ? new ForeignKeyConstraint(referencesAttr.Type) : null
                        : new ForeignKeyConstraint(fkAttr.Type, fkAttr.OnDelete, fkAttr.OnUpdate, fkAttr.ForeignKeyName),
                    IsReference = isReference,
                    GetValueFn = propertyInfo.CreateGetter(),
                    SetValueFn = propertyInfo.CreateSetter(),
                    Sequence = sequenceAttr?.Name,
                    IsComputed = computeAttr != null || computedAttr != null || customSelectAttr != null,
                    IsPersisted = persistedAttr != null,
                    ComputeExpression = computeAttr != null ? computeAttr.Expression : string.Empty,
                    CustomSelect = customSelectAttr?.Sql,
                    CustomInsert = propertyInfo.FirstAttribute<CustomInsertAttribute>()?.Sql,
                    CustomUpdate = propertyInfo.FirstAttribute<CustomUpdateAttribute>()?.Sql,
                    Scale = decimalAttribute?.Scale,
                    BelongToModelName = belongToAttribute?.BelongToTableType.GetModelDefinition().ModelName,
                    CustomFieldDefinition = customFieldAttr?.Sql,
                    IsRefType = propertyType.IsRefType(),
                    Order = order
                };

                if (isIgnored)
                    modelDef.IgnoredFieldDefinitions.Add(fieldDefinition);
                else
                    modelDef.FieldDefinitions.Add(fieldDefinition);

                if (isRowVersion)
                    modelDef.RowVersion = fieldDefinition;
            }

            modelDef.AfterInit();

            Dictionary<string, ModelDefinition> snapshot, newCache;
            do
            {
                snapshot = typeModelDefinitionMap;
                newCache = new Dictionary<string, ModelDefinition>(typeModelDefinitionMap) { [cacheKey] = modelDef };

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref typeModelDefinitionMap, newCache, snapshot), snapshot));

            LicenseUtils.AssertValidUsage(LicenseFeature.OrmLite, QuotaType.Tables, typeModelDefinitionMap.Count);

            modelDef.Alias = tableName;

            return modelDef;
        }

        public static ModelDefinition CreateInstance<T>(string tableName = null)
        {
            return CreateInstance(typeof(T), tableName);
        }

        #endregion

        public ModelDefinition()
        {
            this.FieldDefinitions = new List<FieldDefinition>();
            this.IgnoredFieldDefinitions = new List<FieldDefinition>();
            this.CompositeIndexes = new List<CompositeIndexAttribute>();
            this.UniqueConstraints = new List<UniqueConstraintAttribute>();
        }

        public const string RowVersionName = "RowVersion";

        public string Name { get; set; }

        public string Alias { get; set; }

        public string Schema { get; set; }

        public string PreCreateTableSql { get; set; }

        public string PostCreateTableSql { get; set; }

        public string PreDropTableSql { get; set; }

        public string PostDropTableSql { get; set; }

        public bool IsInSchema => this.Schema != null;

        public bool HasAutoIncrementId => PrimaryKey != null && PrimaryKey.AutoIncrement;

        public bool HasSequenceAttribute => this.FieldDefinitions.Any(x => !x.Sequence.IsNullOrEmpty());

        public FieldDefinition RowVersion { get; set; }

        public string ModelName => this.Alias ?? this.Name;

        public Type ModelType { get; set; }

        public FieldDefinition PrimaryKey
        {
            get { return this.FieldDefinitions.First(x => x.IsPrimaryKey); }
        }

        public object GetPrimaryKey(object instance)
        {
            var pk = PrimaryKey;
            return pk != null
                ? pk.GetValue(instance)
                : instance.GetId();
        }

        public List<FieldDefinition> FieldDefinitions { get; set; }

        public FieldDefinition[] FieldDefinitionsArray { get; private set; }

        public FieldDefinition[] FieldDefinitionsWithAliases { get; private set; }

        public List<FieldDefinition> IgnoredFieldDefinitions { get; set; }

        public FieldDefinition[] IgnoredFieldDefinitionsArray { get; private set; }

        public FieldDefinition[] AllFieldDefinitionsArray { get; private set; }

        private readonly object fieldDefLock = new object();
        private Dictionary<string, FieldDefinition> fieldDefinitionMap;
        private Func<string, string> fieldNameSanitizer;

        public FieldDefinition[] AutoIdFields { get; private set; }

        public List<FieldDefinition> GetAutoIdFieldDefinitions()
        {
            var to = new List<FieldDefinition>();
            foreach (var fieldDef in FieldDefinitionsArray)
            {
                if (fieldDef.AutoId)
                {
                    to.Add(fieldDef);
                }
            }
            return to;
        }

        public FieldDefinition[] GetOrderedFieldDefinitions(ICollection<string> fieldNames, Func<string, string> sanitizeFieldName = null)
        {
            if (fieldNames == null)
                throw new ArgumentNullException(nameof(fieldNames));

            var fieldDefs = new FieldDefinition[fieldNames.Count];

            var i = 0;
            foreach (var fieldName in fieldNames)
            {
                var fieldDef = sanitizeFieldName != null
                    ? AssertFieldDefinition(fieldName, sanitizeFieldName)
                    : AssertFieldDefinition(fieldName);
                fieldDefs[i++] = fieldDef;
            }

            return fieldDefs;
        }

        public Dictionary<string, FieldDefinition> GetFieldDefinitionMap(Func<string, string> sanitizeFieldName)
        {
            lock (fieldDefLock)
            {
                if (fieldDefinitionMap != null && fieldNameSanitizer == sanitizeFieldName)
                    return fieldDefinitionMap;

                fieldDefinitionMap = new Dictionary<string, FieldDefinition>(StringComparer.OrdinalIgnoreCase);
                fieldNameSanitizer = sanitizeFieldName;
                foreach (var fieldDef in FieldDefinitionsArray)
                {
                    fieldDefinitionMap[sanitizeFieldName(fieldDef.FieldName)] = fieldDef;
                }
                return fieldDefinitionMap;
            }
        }

        public List<CompositeIndexAttribute> CompositeIndexes { get; set; }

        public List<UniqueConstraintAttribute> UniqueConstraints { get; set; }

        public FieldDefinition GetFieldDefinition<T>(Expression<Func<T, object>> field)
        {
            return GetFieldDefinition(ExpressionUtils.GetMemberName(field));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowNoFieldException(string fieldName) =>
            throw new NotSupportedException($"'{fieldName}' is not a property of '{Name}'");

        public FieldDefinition AssertFieldDefinition(string fieldName)
        {
            var fieldDef = GetFieldDefinition(fieldName);
            if (fieldDef == null)
                ThrowNoFieldException(fieldName);

            return fieldDef;
        }

        public FieldDefinition GetFieldDefinition(string fieldName)
        {
            if (fieldName != null)
            {
                foreach (var f in FieldDefinitionsWithAliases)
                {
                    if (f.Alias == fieldName)
                        return f;
                }
                foreach (var f in FieldDefinitionsArray)
                {
                    if (f.Name == fieldName)
                        return f;
                }
                foreach (var f in FieldDefinitionsWithAliases)
                {
                    if (string.Equals(f.Alias, fieldName, StringComparison.OrdinalIgnoreCase))
                        return f;
                }
                foreach (var f in FieldDefinitionsArray)
                {
                    if (string.Equals(f.Name, fieldName, StringComparison.OrdinalIgnoreCase))
                        return f;
                }
            }
            return null;
        }

        public FieldDefinition AssertFieldDefinition(string fieldName, Func<string, string> sanitizeFieldName)
        {
            var fieldDef = GetFieldDefinition(fieldName, sanitizeFieldName);
            if (fieldDef == null)
                ThrowNoFieldException(fieldName);

            return fieldDef;
        }

        public FieldDefinition GetFieldDefinition(string fieldName, Func<string, string> sanitizeFieldName)
        {
            if (fieldName != null)
            {
                foreach (var f in FieldDefinitionsWithAliases)
                {
                    if (f.Alias == fieldName || sanitizeFieldName(f.Alias) == fieldName)
                        return f;
                }
                foreach (var f in FieldDefinitionsArray)
                {
                    if (f.Name == fieldName || sanitizeFieldName(f.Name) == fieldName)
                        return f;
                }
                foreach (var f in FieldDefinitionsWithAliases)
                {
                    if (string.Equals(f.Alias, fieldName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sanitizeFieldName(f.Alias), fieldName, StringComparison.OrdinalIgnoreCase))
                        return f;
                }
                foreach (var f in FieldDefinitionsArray)
                {
                    if (string.Equals(f.Name, fieldName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sanitizeFieldName(f.Name), fieldName, StringComparison.OrdinalIgnoreCase))
                        return f;
                }
            }
            return null;
        }

        public string GetQuotedName(string fieldName, IOrmLiteDialectProvider dialectProvider) =>
            GetFieldDefinition(fieldName)?.GetQuotedName(dialectProvider);

        public FieldDefinition GetFieldDefinition(Func<string, bool> predicate)
        {
            foreach (var f in FieldDefinitionsWithAliases)
            {
                if (predicate(f.Alias))
                    return f;
            }
            foreach (var f in FieldDefinitionsArray)
            {
                if (predicate(f.Name))
                    return f;
            }

            return null;
        }

        public void AfterInit()
        {
            FieldDefinitionsArray = FieldDefinitions.ToArray();
            FieldDefinitionsWithAliases = FieldDefinitions.Where(x => x.Alias != null).ToArray();

            IgnoredFieldDefinitionsArray = IgnoredFieldDefinitions.ToArray();

            var allItems = new List<FieldDefinition>(FieldDefinitions);
            allItems.AddRange(IgnoredFieldDefinitions);
            AllFieldDefinitionsArray = allItems.ToArray();

            AutoIdFields = GetAutoIdFieldDefinitions().ToArray();

            OrmLiteConfig.OnModelDefinitionInit?.Invoke(this);
        }

        public bool IsRefField(FieldDefinition fieldDef)
        {
            return (fieldDef.Alias != null && IsRefField(fieldDef.Alias))
                    || IsRefField(fieldDef.Name);
        }

        private bool IsRefField(string name)
        {
            return (Alias != null && Alias + "Id" == name)
                    || Name + "Id" == name;
        }

        public override string ToString()
        {
            return Name;
        }
    }


    public static class ModelDefinition<T>
    {
        private static ModelDefinition definition;
        public static ModelDefinition Definition => definition ??= typeof(T).GetModelDefinition();

        private static string primaryKeyName;
        public static string PrimaryKeyName => primaryKeyName ??= Definition.PrimaryKey.FieldName;
    }
}