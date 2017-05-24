using Sitecore;
using Sitecore.Collections;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Reflection;
using Sitecore.Resources;
using Sitecore.Shell;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Shell.Applications.ContentManager;
using Sitecore.Web.UI.HtmlControls;

namespace Sitecore.Support.Shell.Applications.ContentEditor
{
    public class EditorFormatter : Sitecore.Shell.Applications.ContentEditor.EditorFormatter
    {
        public override void RenderField(System.Web.UI.Control parent, Editor.Field field, Item fieldType, bool readOnly)
        {
            Assert.ArgumentNotNull(parent, "parent");
            Assert.ArgumentNotNull(field, "field");
            Assert.ArgumentNotNull(fieldType, "fieldType");
            string value;
            if (field.ItemField.IsBlobField && !this.Arguments.ShowInputBoxes)
            {
                readOnly = true;
                value = Translate.Text("[Blob Value]");
            }
            else
            {
                value = field.Value;
            }
            this.RenderField(parent, field, fieldType, readOnly, value);
        }

        public override void RenderField(System.Web.UI.Control parent, Editor.Field field, Item fieldType, bool readOnly, string value)
        {
            Assert.ArgumentNotNull(parent, "parent");
            Assert.ArgumentNotNull(field, "field");
            Assert.ArgumentNotNull(fieldType, "fieldType");
            Assert.ArgumentNotNull(value, "value");
            bool flag = false;
            string empty = string.Empty;
            System.Web.UI.Control editor = this.GetEditor(fieldType);
            if (this.Arguments.ShowInputBoxes)
            {
                ChildList children = fieldType.Children;
                flag = (!UserOptions.ContentEditor.ShowRawValues && children["Ribbon"] != null);
                string typeKey = field.TemplateField.TypeKey;
                if (typeKey == "rich text" || typeKey == "html")
                {
                    flag = (flag && UserOptions.HtmlEditor.ContentEditorMode != UserOptions.HtmlEditor.Mode.Preview);
                }
            }
            string text = string.Empty;
            string text2 = string.Empty;
            int @int = Registry.GetInt("/Current_User/Content Editor/Field Size/" + field.TemplateField.ID.ToShortID(), -1);
            if (@int != -1)
            {
                text = string.Format(" height:{0}px", @int);
                Sitecore.Web.UI.HtmlControls.Control control = editor as Sitecore.Web.UI.HtmlControls.Control;
                if (control != null)
                {
                    control.Height = new System.Web.UI.WebControls.Unit((double)@int, System.Web.UI.WebControls.UnitType.Pixel);
                }
                else
                {
                    Sitecore.Web.UI.WebControl webControl = editor as Sitecore.Web.UI.WebControl;
                    if (webControl != null)
                    {
                        webControl.Height = new System.Web.UI.WebControls.Unit((double)@int, System.Web.UI.WebControls.UnitType.Pixel);
                    }
                }
            }
            else if (editor is Frame)
            {
                string style = field.ItemField.Style;
                if (string.IsNullOrEmpty(style) || !style.ToLowerInvariant().Contains("height"))
                {
                    text2 = " class='defaultFieldEditorsFrameContainer'";
                }
            }
            else if (editor is MultilistEx)
            {
                string style2 = field.ItemField.Style;
                if (string.IsNullOrEmpty(style2) || !style2.ToLowerInvariant().Contains("height"))
                {
                    text2 = " class='defaultFieldEditorsMultilistContainer'";
                }
            }
            else
            {
                string typeKey2 = field.ItemField.TypeKey;
                if (!string.IsNullOrEmpty(typeKey2) && typeKey2.Equals("checkbox") && !UserOptions.ContentEditor.ShowRawValues)
                {
                    text2 = "class='scCheckBox'";
                }
            }
            this.AddLiteralControl(parent, string.Concat(new string[]
            {
        "<div style='",
        text,
        "' ",
        text2,
        ">"
            }));
            this.AddLiteralControl(parent, empty);
            this.AddEditorControl(parent, editor, field, flag, readOnly, value);
            this.AddLiteralControl(parent, "</div>");
            this.RenderResizable(parent, field);
        }

        private void RenderResizable(System.Web.UI.Control parent, Editor.Field field)
        {
            string type = field.TemplateField.Type;
            if (string.IsNullOrEmpty(type))
            {
                return;
            }
            FieldType fieldType = FieldTypeManager.GetFieldType(type);
            if (fieldType == null || !fieldType.Resizable)
            {
                return;
            }
            string text = string.Concat(new object[]
            {
        "<div style=\"cursor:row-resize; position: relative; height: 5px; width: 100%; top: 0px; left: 0px;\" onmousedown=\"scContent.fieldResizeDown(this, event)\" onmousemove=\"scContent.fieldResizeMove(this, event)\" onmouseup=\"scContent.fieldResizeUp(this, event, '",
        field.TemplateField.ID.ToShortID(),
        "')\">",
        Images.GetSpacer(1, 4),
        "</div>"
            });
            this.AddLiteralControl(parent, text);
            text = "<div class style=\"display:none\" \">" + Images.GetSpacer(1, 4) + "</div>";
            this.AddLiteralControl(parent, text);
        }

        public override void AddEditorControl(System.Web.UI.Control parent, System.Web.UI.Control editor, Editor.Field field, bool hasRibbon, bool readOnly, string value)
        {
            Assert.ArgumentNotNull(parent, "parent");
            Assert.ArgumentNotNull(editor, "editor");
            Assert.ArgumentNotNull(field, "field");
            Assert.ArgumentNotNull(value, "value");
            EditorFormatter.SetProperties(editor, field, readOnly);
            this.SetValue(editor, value);
            System.Web.UI.Control control = new EditorFieldContainer(editor)
            {
                ID = field.ControlID + "_container"
            };
            Context.ClientPage.AddControl(parent, control);
            EditorFormatter.SetXProperties(editor, field, readOnly);
            EditorFormatter.SetAttributes(editor, field, hasRibbon);
            EditorFormatter.SetStyle(editor, field);
            this.SetValue(editor, value);
        }

        public static void SetXProperties(System.Web.UI.Control editor, Editor.Field field, bool readOnly)
        {
            Assert.ArgumentNotNull(editor, "editor");
            Assert.ArgumentNotNull(field, "field");
            ReflectionUtil.SetProperty(editor, "ID", field.ControlID);
            ReflectionUtil.SetProperty(editor, "ItemID", field.ItemField.Item.ID.ToString());
            ReflectionUtil.SetProperty(editor, "ItemVersion", field.ItemField.Item.Version.ToString());
            ReflectionUtil.SetProperty(editor, "ItemLanguage", field.ItemField.Item.Language.ToString());
            ReflectionUtil.SetProperty(editor, "FieldID", field.ItemField.ID.ToString());
            ReflectionUtil.SetProperty(editor, "Source", field.ItemField.Source);
            ReflectionUtil.SetProperty(editor, "ReadOnly", readOnly);
            ReflectionUtil.SetProperty(editor, "Disabled", readOnly);
        }
    }
}