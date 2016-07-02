﻿using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OfficeDevPnP.Core.Tests.Framework.Functional.Validators
{
    #region Delegates
    public delegate void ValidateEventHandler(object sender, ValidateEventArgs e);
    #endregion

    /// <summary>
    /// Base object validator class
    /// </summary>
    public class ValidatorBase
    {
        #region Events
        public event ValidateEventHandler ValidateEvent;
        #endregion

        #region Validation methods
        /// <summary>
        /// Validate two collection objects
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceElement"></param>
        /// <param name="targetElement"></param>
        /// <param name="props"></param>
        /// <returns></returns>
        public bool ValidateObjects<T>(T sourceElement, T targetElement, List<string> properties, TokenParser tokenParser=null, List<string> parsedProperties=null) where T : class
        {
            IEnumerable sElements = (IEnumerable)sourceElement;
            IEnumerable tElements = (IEnumerable)targetElement;

            string key = properties[0];
            int sourceCount = 0;
            int targetCount = 0;
            foreach (object sElem in sElements)
            {
                sourceCount++;
                string sourceKey = sElem.GetType().GetProperty(key).GetValue(sElem).ToString();

                if (tokenParser != null && parsedProperties != null)
                {
                    if (parsedProperties.Contains(key))
                    {
                        sourceKey = tokenParser.ParseString(Convert.ToString(sourceKey));
                    }
                }

                foreach (object tElem in tElements)
                {
                    string targetKey = tElem.GetType().GetProperty(key).GetValue(tElem).ToString();

                    if (sourceKey.Equals(targetKey))
                    {
                        targetCount++;
                        //compare objects
                        foreach(string property in properties)
                        {
                            string sourceProperty = sElem.GetType().GetProperty(property).GetValue(sElem).ToString();
                            if (tokenParser != null && parsedProperties != null)
                            {
                                if (parsedProperties.Contains(property))
                                {
                                    sourceProperty = tokenParser.ParseString(Convert.ToString(sourceProperty));
                                }
                            }

                            string targetProperty = tElem.GetType().GetProperty(property).GetValue(tElem).ToString();

                            ValidateEventArgs e = null;
                            if (ValidateEvent != null)
                            {
                                e = new ValidateEventArgs(property, sourceProperty, targetProperty, sElem, tElem);
                                ValidateEvent(this, e);
                            }

                            if (e != null && e.IsEqual)
                            {
                                // Do nothing since we've declared equality in the event handler
                            }
                            else
                            {
                                if (!sourceProperty.Equals(targetProperty))
                                {
                                    return false;
                                }
                            }
                        }
                        break;
                    }
                }
            }

            return sourceCount == targetCount;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceParser"></param>
        /// <param name="targetParser"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public bool ValidateObjectSchemaXML<T>(TokenParser sourceParser, TokenParser targetParser, IEnumerable<T> source, IEnumerable<T> target, string property) where T : class
        {
            int scount = 0;
            int tcount = 0;

            foreach (var sField in source)
            {
                object sSchemaXml = sField.GetType().GetProperty("SchemaXml").GetValue(sField);
                XElement sourceElement = XElement.Parse(sourceParser.ParseString(sSchemaXml.ToString(), "~sitecollection", "~site"));
                var sValue = sourceElement.Attribute(property).Value;
                scount++;

                foreach (var tField in target)
                {
                    object tSchemaXml = sField.GetType().GetProperty("SchemaXml").GetValue(sField);
                    XElement targetElement = XElement.Parse(targetParser.ParseString(tSchemaXml.ToString(), "~sitecollection", "~site"));
                    var tValue = targetElement.Attribute(property).Value;

                    if (sValue == tValue)
                    {
                        tcount++;
                        break;
                    }
                }
            }

            if (scount != tcount)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="security"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool ValidateSecurity(ClientContext context, ObjectSecurity security, SecurableObject item)
        {
            int dataRowRoleAssignmentCount = security.RoleAssignments.Count;
            int roleCount = 0;

            IEnumerable roles = context.LoadQuery(item.RoleAssignments.Include(roleAsg => roleAsg.Member,
                roleAsg => roleAsg.RoleDefinitionBindings.Include(roleDef => roleDef.Name)));
            context.ExecuteQuery();

            foreach (var s in security.RoleAssignments)
            {
                foreach (Microsoft.SharePoint.Client.RoleAssignment r in roles)
                {
                    if (r.Member.LoginName.Contains(s.Principal) && r.RoleDefinitionBindings.Where(i => i.Name == s.RoleDefinition).FirstOrDefault() != null)
                    {
                        roleCount++;
                    }
                }
            }

            if (dataRowRoleAssignmentCount != roleCount)
            {
                return false;
            }

            return true;
        }
        #endregion

    }
}
