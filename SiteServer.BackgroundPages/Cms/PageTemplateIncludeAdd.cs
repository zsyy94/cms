﻿using System;
using System.Collections.Specialized;
using System.Web.UI.WebControls;
using BaiRong.Core;
using BaiRong.Core.Model.Enumerations;
using SiteServer.CMS.Core;

namespace SiteServer.BackgroundPages.Cms
{
	public class PageTemplateIncludeAdd : BasePageCms
    {
        public Literal LtlPageTitle;
        public TextBox TbRelatedFileName;
        public Literal LtlCreatedFileExtName;
        public DropDownList DdlCharset;
        public TextBox TbContent;
        public PlaceHolder PhCodeMirror;
        public Button BtnEditorType;

        private string _fileName;
        private string _directoryPath;

        public static string GetRedirectUrl(int publishmentSystemId)
        {
            return PageUtils.GetCmsUrl(nameof(PageTemplateIncludeAdd), new NameValueCollection
            {
                {"PublishmentSystemID", publishmentSystemId.ToString()}
            });
        }

        public static string GetRedirectUrl(int publishmentSystemId, string fileName)
        {
            return PageUtils.GetCmsUrl(nameof(PageTemplateIncludeAdd), new NameValueCollection
            {
                {"PublishmentSystemID", publishmentSystemId.ToString()},
                {"FileName", fileName}
            });
        }

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            PageUtils.CheckRequestParameter("PublishmentSystemID");

            if (Body.IsQueryExists("FileName"))
            {
                _fileName = Body.GetQueryString("FileName");
                _fileName = PathUtils.RemoveParentPath(_fileName);
            }
            _directoryPath = PathUtility.MapPath(PublishmentSystemInfo, "@/include");

            if (IsPostBack) return;

            VerifySitePermissions(AppManager.Permissions.WebSite.Template);

            LtlPageTitle.Text = string.IsNullOrEmpty(_fileName) ? "添加包含文件" : "编辑包含文件";

            var isCodeMirror = PublishmentSystemInfo.Additional.ConfigTemplateIsCodeMirror;
            BtnEditorType.Text = isCodeMirror ? "采用纯文本编辑模式" : "采用代码编辑模式";
            PhCodeMirror.Visible = isCodeMirror;

            ECharsetUtils.AddListItems(DdlCharset);

            if (_fileName != null)
            {
                if (!EFileSystemTypeUtils.IsHtml(PathUtils.GetExtension(_fileName)))
                {
                    PageUtils.RedirectToErrorPage("对不起，此文件无法编辑！");
                }
                else
                {
                    TbRelatedFileName.Text = PathUtils.RemoveExtension(_fileName);
                    LtlCreatedFileExtName.Text = PathUtils.GetExtension(_fileName);
                    var fileCharset = FileUtils.GetFileCharset(PathUtils.Combine(_directoryPath, _fileName));
                    ControlUtils.SelectSingleItemIgnoreCase(DdlCharset, ECharsetUtils.GetValue(fileCharset));
                    TbContent.Text = FileUtils.ReadText(PathUtils.Combine(_directoryPath, _fileName), fileCharset);
                }
            }
            else
            {
                LtlCreatedFileExtName.Text = ".html";
                ControlUtils.SelectSingleItemIgnoreCase(DdlCharset, PublishmentSystemInfo.Additional.Charset);
            }
        }

        public void EditorType_OnClick(object sender, EventArgs e)
        {
            if (!Page.IsPostBack || !Page.IsValid) return;

            var isCodeMirror = PublishmentSystemInfo.Additional.ConfigTemplateIsCodeMirror;
            isCodeMirror = !isCodeMirror;
            PublishmentSystemInfo.Additional.ConfigTemplateIsCodeMirror = isCodeMirror;
            DataProvider.PublishmentSystemDao.Update(PublishmentSystemInfo);

            BtnEditorType.Text = isCodeMirror ? "采用纯文本编辑模式" : "采用代码编辑模式";
            PhCodeMirror.Visible = isCodeMirror;
        }

        public override void Submit_OnClick(object sender, EventArgs e)
        {
            if (!Page.IsPostBack || !Page.IsValid) return;

            if (_fileName != null)
            {
                var isChanged = false;
                if (PathUtils.RemoveExtension(_fileName) != PathUtils.RemoveExtension(TbRelatedFileName.Text))//文件名改变
                {
                    var fileNames = DirectoryUtils.GetFileNames(_directoryPath);
                    foreach (var theFileName in fileNames)
                    {
                        var fileNameWithoutExtension = PathUtils.RemoveExtension(theFileName);
                        if (fileNameWithoutExtension == TbRelatedFileName.Text.ToLower())
                        {
                            FailMessage("包含文件修改失败，包含文件已存在！");
                            return;
                        }
                    }

                    isChanged = true;
                }

                if (PathUtils.GetExtension(_fileName) != LtlCreatedFileExtName.Text)//文件后缀改变
                {
                    isChanged = true;
                }

                var previousFileName = string.Empty;
                if (isChanged)
                {
                    previousFileName = _fileName;
                }

                var currentFileName = TbRelatedFileName.Text + LtlCreatedFileExtName.Text;
                var charset = ECharsetUtils.GetEnumType(DdlCharset.SelectedValue);
                FileUtils.WriteText(PathUtils.Combine(_directoryPath, currentFileName), charset, TbContent.Text);
                if (!string.IsNullOrEmpty(previousFileName))
                {
                    FileUtils.DeleteFileIfExists(PathUtils.Combine(_directoryPath, previousFileName));
                }
                Body.AddSiteLog(PublishmentSystemId, "修改包含文件", $"包含文件:{currentFileName}");
                SuccessMessage("包含文件修改成功！");
                AddWaitAndRedirectScript(PageTemplateInclude.GetRedirectUrl(PublishmentSystemId));
            }
            else
            {
                var currentFileName = TbRelatedFileName.Text + LtlCreatedFileExtName.Text;

                var fileNames = DirectoryUtils.GetFileNames(_directoryPath);
                foreach (var theFileName in fileNames)
                {
                    if (StringUtils.EqualsIgnoreCase(theFileName, currentFileName))
                    {
                        FailMessage("包含文件添加失败，包含文件文件已存在！");
                        return;
                    }
                }

                var charset = ECharsetUtils.GetEnumType(DdlCharset.SelectedValue);
                FileUtils.WriteText(PathUtils.Combine(_directoryPath, currentFileName), charset, TbContent.Text);
                Body.AddSiteLog(PublishmentSystemId, "添加包含文件", $"包含文件:{currentFileName}");
                SuccessMessage("包含文件添加成功！");
                AddWaitAndRedirectScript(PageTemplateInclude.GetRedirectUrl(PublishmentSystemId));
            }
        }

        public void Return_OnClick(object sender, EventArgs e)
        {
            PageUtils.Redirect(PageTemplateInclude.GetRedirectUrl(PublishmentSystemId));
        }
    }
}
