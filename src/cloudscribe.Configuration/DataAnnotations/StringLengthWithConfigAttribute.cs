﻿// Author:					Joe Audette
// Created:					2014-10-28
// Last Modified:			2014-10-30
// 

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Web.Mvc;

namespace cloudscribe.Configuration.DataAnnotations
{
    /// <summary>
    /// Similar to StringLengthAttribute but allows overriding the Min and Max lengths from appSettings
    /// rather than being hard coded, but if the keys are not found then the hard coded value is used.
    /// 
    /// Note that a zero length string is allowed no matter what the minimum length is.
    /// You must use a separate Required attribute if you don't want to allow empty values.
    /// So you can support a scenario where a value is not required but if provided must meet a specific length criteria.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class StringLengthWithConfigAttribute : ValidationAttribute , IClientValidatable 
    {
        public StringLengthWithConfigAttribute()
        { }

        public StringLengthWithConfigAttribute(
            int minimumLength, 
            int maximumLength,
            string minLengthKey,
            string maxLengthKey)
        {
            MinimumLength = minimumLength;
            MaximumLength = maximumLength;
            MinLengthKey = minLengthKey;
            MaxLengthKey = maxLengthKey;
        }

        public int MaximumLength { get; set; }
        public int MinimumLength { get; set; }
        public string MinLengthKey { get; set; }
        public string MaxLengthKey { get; set; }

        public override bool IsValid(object value)
        {
            if (!(value is string)) { return false; }
            int length = value.ToString().Length;

            MinimumLength = AppSettings.GetInt(MinLengthKey, MinimumLength);
            MaximumLength = AppSettings.GetInt(MaxLengthKey, MaximumLength);

            if (length < MinimumLength) { return false; }
            if (length > MaximumLength) { return false; }


            return true;
        }

        public override string FormatErrorMessage(string name)
        {
            if(
                (ErrorMessageResourceName.Length > 0)
                &&(ErrorMessageResourceType != null)
                )
            {
                // we don't need to pass in the property name since it should be in the resource string
                return string.Format(
                    CultureInfo.CurrentCulture,
                    ErrorMessageString, new object[] { MinimumLength, MaximumLength });
            }
                
            // here we are passing in the name of the property
            string errorMessageFormat = "{0} must be between {1} and {2}";
            return string.Format(
                CultureInfo.CurrentCulture,
                errorMessageFormat, new object[] { name, MinimumLength, MaximumLength });

        }

        #region IClientValidatable Members

        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context)
        {
            MinimumLength = AppSettings.GetInt(MinLengthKey, MinimumLength);
            MaximumLength = AppSettings.GetInt(MaxLengthKey, MaximumLength);

            string errorMessageFormat = ErrorMessageString;
            if (string.IsNullOrEmpty(errorMessageFormat))
            {
                errorMessageFormat = metadata.DisplayName + " length must be between {0} and {1}";
             
            }

            var rules = new ModelClientValidationStringLengthRule(errorMessageFormat, MinimumLength, MaximumLength);
            yield return rules;
        }

        #endregion
    }
}