﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web.UI.WebControls;
using SiteServer.Abstractions;
using SiteServer.CMS.Context;
using SiteServer.CMS.Core;
using SiteServer.CMS.DataCache;
using SiteServer.CMS.Repositories;
using TableStyle = SiteServer.Abstractions.TableStyle;

namespace SiteServer.BackgroundPages.Cms
{
    public class PageTableStyleContent : BasePageCms
    {
        public DropDownList DdlChannelId;
        public Repeater RptContents;

        public Button BtnAddStyle;
        public Button BtnAddStyles;
        public Button BtnImport;
        public Button BtnExport;

        private Channel _channel;
        private string _tableName;
        private List<int> _relatedIdentities;
        private string _redirectUrl;

        public static string GetRedirectUrl(int siteId, int channelId)
        {
            return PageUtils.GetCmsUrl(siteId, nameof(PageTableStyleContent), new NameValueCollection
            {
                {"channelId", channelId.ToString()}
            });
        }

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            var channelId = AuthRequest.GetQueryInt("channelId", SiteId);
            _channel = ChannelManager.GetChannelAsync(SiteId, channelId).GetAwaiter().GetResult();
            _tableName = ChannelManager.GetTableNameAsync(Site, _channel).GetAwaiter().GetResult();
            _redirectUrl = GetRedirectUrl(SiteId, channelId);
            _relatedIdentities = TableStyleManager.GetRelatedIdentities(_channel);

            if (IsPostBack) return;

            VerifySitePermissions(Constants.SitePermissions.ConfigTableStyles);

            //删除样式
            if (AuthRequest.IsQueryExists("DeleteStyle"))
            {
                var attributeName = AuthRequest.GetQueryString("AttributeName");
                if (TableStyleManager.IsExistsAsync(_channel.Id, _tableName, attributeName).GetAwaiter().GetResult())
                {
                    try
                    {
                        DataProvider.TableStyleRepository.DeleteAsync(_channel.Id, _tableName, attributeName).GetAwaiter().GetResult();
                        AuthRequest.AddSiteLogAsync(SiteId, "删除数据表单样式", $"表单:{_tableName},字段:{attributeName}").GetAwaiter().GetResult();
                        SuccessDeleteMessage();
                    }
                    catch (Exception ex)
                    {
                        FailDeleteMessage(ex);
                    }
                }
            }

            InfoMessage(
                $"在此编辑内容模型字段,子栏目默认继承父栏目字段设置; 内容表:{_tableName}");
            ChannelManager.AddListItemsAsync(DdlChannelId.Items, Site, false, true, AuthRequest.AdminPermissionsImpl).GetAwaiter().GetResult();
            ControlUtils.SelectSingleItem(DdlChannelId, channelId.ToString());

            RptContents.DataSource = TableStyleManager.GetContentStyleListAsync(Site, _channel).GetAwaiter().GetResult();
            RptContents.ItemDataBound += RptContents_ItemDataBound;
            RptContents.DataBind();

            BtnAddStyle.Attributes.Add("onclick", ModalTableStyleAdd.GetOpenWindowString(SiteId, 0, _relatedIdentities, _tableName, string.Empty, _redirectUrl));
            BtnAddStyles.Attributes.Add("onclick", ModalTableStylesAdd.GetOpenWindowString(SiteId, _relatedIdentities, _tableName, _redirectUrl));
            BtnImport.Attributes.Add("onclick", ModalTableStyleImport.GetOpenWindowString(_tableName, SiteId, channelId));
            BtnExport.Attributes.Add("onclick", ModalExportMessage.GetOpenWindowStringToSingleTableStyle(_tableName, SiteId, channelId));
        }

        public void Redirect(object sender, EventArgs e)
        {
            PageUtils.Redirect(GetRedirectUrl(SiteId, TranslateUtils.ToInt(DdlChannelId.SelectedValue)));
        }

        private void RptContents_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem) return;

            var style = (TableStyle)e.Item.DataItem;

            var ltlAttributeName = (Literal)e.Item.FindControl("ltlAttributeName");
            var ltlDisplayName = (Literal)e.Item.FindControl("ltlDisplayName");
            var ltlInputType = (Literal)e.Item.FindControl("ltlInputType");
            var ltlFieldType = (Literal)e.Item.FindControl("ltlFieldType");
            var ltlValidate = (Literal)e.Item.FindControl("ltlValidate");
            var ltlTaxis = (Literal)e.Item.FindControl("ltlTaxis");
            var ltlEditStyle = (Literal)e.Item.FindControl("ltlEditStyle");
            var ltlEditValidate = (Literal)e.Item.FindControl("ltlEditValidate");

            ltlAttributeName.Text = style.AttributeName;

            ltlDisplayName.Text = style.DisplayName;
            ltlInputType.Text = InputTypeUtils.GetText(style.Type);

            var columnInfo = TableColumnManager.GetTableColumnInfoAsync(_tableName, style.AttributeName).GetAwaiter().GetResult();

            ltlFieldType.Text = columnInfo != null ? $"真实 {DataTypeUtils.GetText(columnInfo.DataType)}" : "虚拟字段";

            ltlValidate.Text = TableStyleManager.GetValidateInfo(style);

            if (!StringUtils.EqualsIgnoreCase(style.AttributeName, ContentAttribute.Title))
            {
                var showPopWinString = ModalTableStyleAdd.GetOpenWindowString(SiteId, style.Id, _relatedIdentities, _tableName, style.AttributeName, _redirectUrl);

                ltlEditStyle.Text = $@"<a href=""javascript:;"" onclick=""{showPopWinString}"">设置</a>";

                showPopWinString = ModalTableStyleValidateAdd.GetOpenWindowString(SiteId, style.Id, _relatedIdentities, _tableName, style.AttributeName, _redirectUrl);
                ltlEditValidate.Text = $@"<a href=""javascript:;"" onclick=""{showPopWinString}"">设置</a>";
            }

            ltlTaxis.Text = style.Taxis.ToString();

            if (style.RelatedIdentity != _channel.Id) return;

            var urlStyle = PageUtils.GetCmsUrl(SiteId, nameof(PageTableStyleContent), new NameValueCollection
            {
                {"channelId", _channel.Id.ToString()},
                {"DeleteStyle", true.ToString()},
                {"TableName", _tableName},
                {"AttributeName", style.AttributeName}
            });
            ltlEditStyle.Text +=
                $@"&nbsp;&nbsp;<a href=""{urlStyle}"" onClick=""javascript:return confirm('此操作将删除对应显示样式，确认吗？');"">删除</a>";
        }
    }
}
