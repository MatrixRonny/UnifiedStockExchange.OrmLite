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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace ServiceStack.OrmLite
{
    internal static class OrmLiteConfigExtensions
    {
        internal static ModelDefinition GetModelDefinition(this Type modelType, string tableName = null)
        {
            return ModelDefinition.CreateInstance(modelType, tableName);
        }

        public static StringLengthAttribute CalculateStringLength(this PropertyInfo propertyInfo, DecimalLengthAttribute decimalAttribute)
        {
            var attr = propertyInfo.FirstAttribute<StringLengthAttribute>();
            if (attr != null) return attr;

            var componentAttr = propertyInfo.FirstAttribute<System.ComponentModel.DataAnnotations.StringLengthAttribute>();
            if (componentAttr != null)
                return new StringLengthAttribute(componentAttr.MaximumLength);

            return decimalAttribute != null ? new StringLengthAttribute(decimalAttribute.Precision) : null;
        }

    }
}