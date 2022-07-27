using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static Afiniti.Paycom.Shared.Enums;

namespace Afiniti.PaycomEngine.Helpers
{
    public class FilterLinq<T>
    {
        public Expression<Func<T, Boolean>> GetWherePredicate(List<Afiniti.Paycom.DAL.ConfigurationSettingDetail> configurationSettings)
        {
            ParameterExpression pe = Expression.Parameter(typeof(T), typeof(T).Name);
            Expression combined = null;
            if (configurationSettings.Count > 0)
            {
                foreach (var predicateItem in configurationSettings)
                {
                    if (predicateItem.ParamName != "")
                    {
                        //   foreach (var predicateValues in predicateItem.ConfigValue.Split(','))
                        //   {
                        Expression columnNameProperty = Expression.Property(pe, predicateItem.ParamName.Trim());
                        Expression columnValue = Expression.Constant(predicateItem.ParamValue.Trim());
                        Enum.TryParse(predicateItem.AdditionalParamName.Trim(), out CriteriaComparison CurrentComparison);
                        switch (CurrentComparison)
                        {
                            case CriteriaComparison.Include:
                                Expression e1 = Expression.Equal(columnNameProperty, columnValue);
                                if (combined == null)
                                {
                                    combined = e1;
                                }
                                else
                                {
                                    combined = Expression.And(combined, e1);
                                }
                                break;
                            case CriteriaComparison.Exclude:
                                Expression e2 = Expression.NotEqual(columnNameProperty, columnValue);
                                if (combined == null)
                                {
                                    combined = e2;
                                }
                                else
                                {
                                    combined = Expression.And(combined, e2);
                                }
                                break;
                            default:
                                break;
                        }
                        //   }
                    }
                }
            }
            return Expression.Lambda<Func<T, Boolean>>(combined, new ParameterExpression[] { pe });
        }

        public Expression<Func<T, Boolean>> GetWherePredicate(string whereFieldList, string whereFieldValues, string Comparison)
        {
            ParameterExpression pe = Expression.Parameter(typeof(T), typeof(T).Name);
            Expression combined = null;
            if (whereFieldList != null && whereFieldValues != null && Comparison != null)
            {
                string[] field = whereFieldList.Split(',');
                string[] fieldValue = whereFieldValues.Split(',');
                string[] comparisonValue = Comparison.Split(',');

                for (int i = 0; i < field.Count(); i++)
                {
                    Expression columnNameProperty = Expression.Property(pe, field[i].Trim());
                    Expression columnValue = Expression.Constant(fieldValue[i].Trim());
                    Enum.TryParse(comparisonValue[i].Trim(), out CriteriaComparison CurrentComparison);

                    switch (CurrentComparison)
                    {
                        case CriteriaComparison.Include:
                            Expression e1 = Expression.Equal(columnNameProperty, columnValue);
                            if (combined == null)
                            {
                                combined = e1;
                            }
                            else
                            {
                                combined = Expression.And(combined, e1);
                            }
                            break;
                        case CriteriaComparison.Exclude:
                            Expression e2 = Expression.NotEqual(columnNameProperty, columnValue);
                            if (combined == null)
                            {
                                combined = e2;
                            }
                            else
                            {
                                combined = Expression.And(combined, e2);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            return Expression.Lambda<Func<T, Boolean>>(combined, new ParameterExpression[] { pe });
        }
        public Expression<Func<T, Boolean>> GetWherePredicate(string whereField)
        {
            ParameterExpression pe = Expression.Parameter(typeof(T), typeof(T).Name);
            Expression combined = null;
            if (whereField != null)
            {
                List<string> fieldValues = new List<string> { "", null };

                for (int i = 0; i < fieldValues.Count(); i++)
                {
                    Expression columnNameProperty = Expression.Property(pe, whereField.Trim());
                    Expression columnValue = Expression.Constant(fieldValues[i]);
                    Enum.TryParse("Include", out CriteriaComparison CurrentComparison);

                    switch (CurrentComparison)
                    {
                        case CriteriaComparison.Include:
                            Expression e1 = Expression.Equal(columnNameProperty, columnValue);
                            if (combined == null)
                            {
                                combined = e1;
                            }
                            else
                            {
                                combined = Expression.Or(combined, e1);
                            }
                            break;
                        case CriteriaComparison.Exclude:
                            Expression e2 = Expression.NotEqual(columnNameProperty, columnValue);
                            if (combined == null)
                            {
                                combined = e2;
                            }
                            else
                            {
                                combined = Expression.And(combined, e2);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            return Expression.Lambda<Func<T, Boolean>>(combined, new ParameterExpression[] { pe });
        }

    }
}