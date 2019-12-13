﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using SiteServer.Abstractions;
using SiteServer.CMS.Core;
using SiteServer.CMS.Core.Create;
using SiteServer.CMS.DataCache;
using SiteServer.CMS.Context.Enumerations;
using SiteServer.CMS.Repositories;

namespace SiteServer.API.Controllers.Pages.Cms.Create
{
    
    [RoutePrefix("pages/cms/create")]
    public class PagesCreateController : ApiController
    {
        private const string Route = "";
        private const string RouteAll = "all";

        [HttpGet, Route(Route)]
        public async Task<IHttpActionResult> GetList()
        {
            try
            {
                var request = await AuthenticatedRequest.GetAuthAsync();
                var type = request.GetQueryString("type");

                var permission = string.Empty;
                if (type == "index")
                {
                    permission = Constants.SitePermissions.CreateIndex;
                }
                else if (type == "channels")
                {
                    permission = Constants.SitePermissions.CreateChannels;
                }
                else if (type == "contents")
                {
                    permission = Constants.SitePermissions.CreateContents;
                }
                else if (type == "all")
                {
                    permission = Constants.SitePermissions.CreateAll;
                }

                if (!request.IsAdminLoggin ||
                    !await request.AdminPermissionsImpl.HasSitePermissionsAsync(request.SiteId, permission))
                {
                    return Unauthorized();
                }

                var siteId = request.SiteId;
                var parentId = request.GetQueryInt("parentId");
                var site = await DataProvider.SiteRepository.GetAsync(siteId);
                var parent = await ChannelManager.GetChannelAsync(siteId, parentId);
                var countDict = new Dictionary<int, int>
                {
                    [parent.Id] = await DataProvider.ContentRepository.GetCountAsync(site, parent)
                };

                var channelInfoList = new List<Channel>();

                var channelIdList =
                    await ChannelManager.GetChannelIdListAsync(parent, EScopeType.Children, string.Empty, string.Empty, string.Empty);

                foreach (var channelId in channelIdList)
                {
                    var enabled = await request.AdminPermissionsImpl.IsOwningChannelIdAsync(channelId);
                    
                    if (!enabled)
                    {
                        if (!await request.AdminPermissionsImpl.IsDescendantOwningChannelIdAsync(siteId, channelId)) continue;
                    }

                    var channelInfo = await ChannelManager.GetChannelAsync(siteId, channelId);
                    channelInfoList.Add(channelInfo);
                    countDict[channelInfo.Id] = await DataProvider.ContentRepository.GetCountAsync(site, channelInfo);
                }

                return Ok(new
                {
                    Value = channelInfoList,
                    Parent = parent,
                    CountDict = countDict
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        public class CreateParameter
        {
            public int SiteId { get; set; }
            public IEnumerable<int> ChannelIdList { get; set; }
            public bool IsAllChecked { get; set; }
            public bool IsDescendent { get; set; }
            public bool IsChannelPage { get; set; }
            public bool IsContentPage { get; set; }
            public string Scope { get; set; }
        }

        [HttpPost, Route(Route)]
        public async Task<IHttpActionResult> Create([FromBody] CreateParameter parameter)
        {
            try
            {
                var request = await AuthenticatedRequest.GetAuthAsync();
                var type = request.GetQueryString("type");

                var permission = string.Empty;
                if (type == "index")
                {
                    permission = Constants.SitePermissions.CreateIndex;
                }
                else if (type == "channels")
                {
                    permission = Constants.SitePermissions.CreateChannels;
                }
                else if (type == "contents")
                {
                    permission = Constants.SitePermissions.CreateContents;
                }
                else if (type == "all")
                {
                    permission = Constants.SitePermissions.CreateAll;
                }

                if (!request.IsAdminLoggin ||
                    !await request.AdminPermissionsImpl.HasSitePermissionsAsync(parameter.SiteId, permission))
                {
                    return Unauthorized();
                }

                var selectedChannelIdList = new List<int>();

                if (parameter.IsAllChecked)
                {
                    selectedChannelIdList = await ChannelManager.GetChannelIdListAsync(parameter.SiteId);
                }
                else if (parameter.IsDescendent)
                {
                    foreach (var channelId in parameter.ChannelIdList)
                    {
                        selectedChannelIdList.Add(channelId);

                        var channelInfo = await ChannelManager.GetChannelAsync(parameter.SiteId, channelId);
                        if (channelInfo.ChildrenCount > 0)
                        {
                            var descendentIdList = await ChannelManager.GetChannelIdListAsync(channelInfo, EScopeType.Descendant);
                            foreach (var descendentId in descendentIdList)
                            {
                                if (selectedChannelIdList.Contains(descendentId)) continue;

                                selectedChannelIdList.Add(descendentId);
                            }
                        }
                    }
                }
                else
                {
                    selectedChannelIdList.AddRange(parameter.ChannelIdList);
                }

                var channelIdList = new List<int>();

                if (parameter.Scope == "1month" || parameter.Scope == "1day" || parameter.Scope == "2hours")
                {
                    var site = await DataProvider.SiteRepository.GetAsync(parameter.SiteId);
                    var tableName = site.TableName;

                    if (parameter.Scope == "1month")
                    {
                        var lastEditList = DataProvider.ContentRepository.GetChannelIdListCheckedByLastEditDateHour(tableName, parameter.SiteId, 720);
                        foreach (var channelId in lastEditList)
                        {
                            if (selectedChannelIdList.Contains(channelId))
                            {
                                channelIdList.Add(channelId);
                            }
                        }
                    }
                    else if (parameter.Scope == "1day")
                    {
                        var lastEditList = DataProvider.ContentRepository.GetChannelIdListCheckedByLastEditDateHour(tableName, parameter.SiteId, 24);
                        foreach (var channelId in lastEditList)
                        {
                            if (selectedChannelIdList.Contains(channelId))
                            {
                                channelIdList.Add(channelId);
                            }
                        }
                    }
                    else if (parameter.Scope == "2hours")
                    {
                        var lastEditList = DataProvider.ContentRepository.GetChannelIdListCheckedByLastEditDateHour(tableName, parameter.SiteId, 2);
                        foreach (var channelId in lastEditList)
                        {
                            if (selectedChannelIdList.Contains(channelId))
                            {
                                channelIdList.Add(channelId);
                            }
                        }
                    }
                }
                else
                {
                    channelIdList = selectedChannelIdList;
                }

                foreach (var channelId in channelIdList)
                {
                    if (parameter.IsChannelPage)
                    {
                        await CreateManager.CreateChannelAsync(parameter.SiteId, channelId);
                    }
                    if (parameter.IsContentPage)
                    {
                        await CreateManager.CreateAllContentAsync(parameter.SiteId, channelId);
                    }
                }

                return Ok(new { });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost, Route(RouteAll)]
        public async Task<IHttpActionResult> CreateAll([FromBody] CreateParameter parameter)
        {
            try
            {
                var request = await AuthenticatedRequest.GetAuthAsync();
                if (!request.IsAdminLoggin ||
                    !await request.AdminPermissionsImpl.HasSitePermissionsAsync(parameter.SiteId, Constants.SitePermissions.CreateAll))
                {
                    return Unauthorized();
                }

                await CreateManager.CreateByAllAsync(parameter.SiteId);

                return Ok(new { });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
