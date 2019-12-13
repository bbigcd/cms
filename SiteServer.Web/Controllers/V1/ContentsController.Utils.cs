﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using Datory;
using NSwag.Annotations;
using SiteServer.Abstractions;
using SiteServer.CMS.Api.V1;
using SiteServer.CMS.Core;
using SiteServer.CMS.Core.Create;
using SiteServer.CMS.DataCache;
using SiteServer.CMS.Extensions;
using SiteServer.CMS.Plugin;
using SiteServer.CMS.Repositories;
using SqlKata;

namespace SiteServer.API.Controllers.V1
{
    public partial class ContentsController
    {
        private Query GetQuery(int siteId, int? channelId, QueryRequest request)
        {
            var query = Q.Where(nameof(Abstractions.Content.SiteId), siteId).Where(nameof(Abstractions.Content.ChannelId), ">", 0);

            if (channelId.HasValue)
            {
                query.Where(nameof(Abstractions.Content.ChannelId), channelId.Value);
            }

            if (request.Checked.HasValue)
            {
                query.Where(nameof(Abstractions.Content.IsChecked), request.Checked.Value.ToString());
            }
            if (request.Top.HasValue)
            {
                query.Where(nameof(Abstractions.Content.IsTop), request.Top.Value.ToString());
            }
            if (request.Recommend.HasValue)
            {
                query.Where(nameof(Abstractions.Content.IsRecommend), request.Recommend.Value.ToString());
            }
            if (request.Color.HasValue)
            {
                query.Where(nameof(Abstractions.Content.IsColor), request.Color.Value.ToString());
            }
            if (request.Hot.HasValue)
            {
                query.Where(nameof(Abstractions.Content.IsHot), request.Hot.Value.ToString());
            }

            if (request.GroupNames != null)
            {
                query.Where(q =>
                {
                    foreach (var groupName in request.GroupNames)
                    {
                        if (!string.IsNullOrEmpty(groupName))
                        {
                            q
                                .OrWhere(nameof(Abstractions.Content.GroupNameCollection), groupName)
                                .OrWhereLike(nameof(Abstractions.Content.GroupNameCollection), $"{groupName},%")
                                .OrWhereLike(nameof(Abstractions.Content.GroupNameCollection), $"%,{groupName},%")
                                .OrWhereLike(nameof(Abstractions.Content.GroupNameCollection), $"%,{groupName}");
                        }
                    }
                    return q;
                });
            }

            if (request.TagNames != null)
            {
                query.Where(q =>
                {
                    foreach (var tagName in request.TagNames)
                    {
                        if (!string.IsNullOrEmpty(tagName))
                        {
                            q
                                .OrWhere(nameof(Abstractions.Content.Tags), tagName)
                                .OrWhereLike(nameof(Abstractions.Content.Tags), $"{tagName},%")
                                .OrWhereLike(nameof(Abstractions.Content.Tags), $"%,{tagName},%")
                                .OrWhereLike(nameof(Abstractions.Content.Tags), $"%,{tagName}");
                        }
                    }
                    return q;
                });
            }

            if (request.Wheres != null)
            {
                foreach (var where in request.Wheres)
                {
                    if (string.IsNullOrEmpty(where.Operator)) where.Operator = OpEquals;
                    if (StringUtils.EqualsIgnoreCase(where.Operator, OpIn))
                    {
                        query.WhereIn(where.Column, StringUtils.GetStringList(where.Value));
                    }
                    else if (StringUtils.EqualsIgnoreCase(where.Operator, OpNotIn))
                    {
                        query.WhereNotIn(where.Column, StringUtils.GetStringList(where.Value));
                    }
                    else if (StringUtils.EqualsIgnoreCase(where.Operator, OpLike))
                    {
                        query.WhereLike(where.Column, where.Value);
                    }
                    else if (StringUtils.EqualsIgnoreCase(where.Operator, OpNotLike))
                    {
                        query.WhereNotLike(where.Column, where.Value);
                    }
                    else
                    {
                        query.Where(where.Column, where.Operator, where.Value);
                    }
                }
            }

            if (request.Orders != null)
            {
                foreach (var order in request.Orders)
                {
                    if (order.Desc)
                    {
                        query.OrderByDesc(order.Column);
                    }
                    else
                    {
                        query.OrderBy(order.Column);
                    }
                }
            }
            else
            {
                query.OrderByDesc(nameof(Abstractions.Content.IsTop), 
                    nameof(Abstractions.Content.ChannelId),
                    nameof(Abstractions.Content.Taxis),
                    nameof(Abstractions.Content.Id));
            }

            var page = request.Page > 0 ? request.Page : 1;
            var perPage = request.PerPage > 0 ? request.PerPage : 20;

            query.ForPage(page, perPage);

            return query;
        }
    }
}
