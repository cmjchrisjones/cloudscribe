﻿// Copyright (c) Source Tree Solutions, LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Author:					Joe Audette
// Created:					2016-01-04
// Last Modified:			2016-01-05
// 

using cloudscribe.Core.Models;
using cloudscribe.Core.Models.Setup;
using Microsoft.AspNet.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cloudscribe.Core.Web.Components
{
    public class EnsureSiteSetupTask : ISetupTask
    {
        public EnsureSiteSetupTask(
            IHttpContextAccessor contextAccessor,
            SiteManager siteManager)
        {
            this.siteManager = siteManager;
            this.contextAccessor = contextAccessor;
        }

        private SiteManager siteManager;
        private IHttpContextAccessor contextAccessor;

        public async Task DoSetupStep(
            Func<string, bool, Task> output,
            Func<string, bool> appNeedsUpgrade,
            Func<string, Version> schemaVersionLookup,
            Func<string, Version> codeVersionLookup
            )
        {
            bool needsUpgrade = appNeedsUpgrade("cloudscribe-core");
           
            if (needsUpgrade)
            {
                await output("cloudscribe-core needs schema upgrade", false);
                return;
            }

            Version codeVersion = codeVersionLookup("cloudscribe-core");
            Version schemaVersion = schemaVersionLookup("cloudscribe-core");
            if(codeVersion == null)
            {
                await output("cloudscribe-core code version not found", false);
            }
            if (schemaVersion == null)
            {
                await output("cloudscribe-core schema version not found", false);
            }

            if((codeVersion != null)&&(schemaVersion != null))
            {
                if(codeVersion == schemaVersion)
                {
                    var message = string.Format(
                        "cloudscribe-core schema is up to date at version: {0}",
                        codeVersion
                        );

                    await output(message, false);

                }
                else
                {
                    var message = string.Format(
                        "cloudscribe-core code version: {0} is out of sync with schema version: {1}",
                        codeVersion, 
                        schemaVersion
                        );

                    await output(message, false);
                }
            }
            
            

            int existingSiteCount = await siteManager.ExistingSiteCount();

            await output(
                        string.Format(
                        "ExistingSiteCount {0}", //SetupResources.ExistingSiteCountMessage,
                        existingSiteCount.ToString()),
                        false);

            if (existingSiteCount == 0)
            {
                await output("CreatingSite", true);

                SiteSettings newSite = await siteManager.CreateNewSite(true);

                await output("CreatingRolesAndAdminUser", true);

                bool result = await siteManager.CreateRequiredRolesAndAdminUser(newSite);

                existingSiteCount = await siteManager.ExistingSiteCount();
                if(existingSiteCount > 0)
                {
                    await output(BuildHomeLink(), false);
                }
                
            }
            else
            {
                // check here if count of users is 0
                // if something went wrong with creating admin user
                // setup page should try to correct it on subsequent runs
                // ie create an admin user if no users exist
                if (contextAccessor.HttpContext.Request.Host.HasValue)
                {
                    ISiteSettings site = await siteManager.Fetch(contextAccessor.HttpContext.Request.Host.Value);
                    if (site != null)
                    {
                        int roleCount = await siteManager.GetRoleCount(site.SiteId);
                        bool roleResult = true;
                        if (roleCount == 0)
                        {
                            await output("CreatingRoles", true);

                            roleResult = await siteManager.EnsureRequiredRoles(site);
                        }

                        if (roleResult)
                        {
                            int userCount = await siteManager.GetUserCount(site.SiteId);
                            if (userCount == 0)
                            {
                                await output("CreatingAdminUser", true);
                                await siteManager.CreateAdminUser(site);
                            }
                        }
                    }
                }
                
                await output(BuildHomeLink(), false);

            }

            



        }

        private string BuildHomeLink()
        {
            var homeLink = "<a href='" + "/" // TODO: was using Page.ResolveUrl("~/")
                + "' title='" + "Home" //SetupResources.HomeLink 
                + "'>"
                + "Home" // SetupResources.HomeLink 
                + "</a>";

            return homeLink;
        }
        

        //public async Task DoSetupStep(HttpResponse response)
        //{
        //    var message = "Testing an ISetupStep";

        //        await response.WriteAsync(
        //            string.Format("{0} - {1}",
        //            message,
        //            DateTime.UtcNow));

        //    await response.WriteAsync("<br/>");



        //}

    }

    

}
