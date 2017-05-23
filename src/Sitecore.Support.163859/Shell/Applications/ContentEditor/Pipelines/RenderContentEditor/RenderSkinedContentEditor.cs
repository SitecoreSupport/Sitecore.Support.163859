using Sitecore.Configuration;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Sitecore.Shell;
using Sitecore.Shell.Applications.ContentEditor.Pipelines.GetContentEditorSkin;
using Sitecore.Shell.Applications.ContentEditor.Pipelines.RenderContentEditor;
using Sitecore.Shell.Applications.ContentManager;
using Sitecore.Xml;
using System;
using System.IO;
using System.Web.UI;
using System.Xml;

namespace Sitecore.Support.Shell.Applications.ContentEditor.Pipelines.RenderContentEditor
{
    public class RenderSkinedContentEditor
    {
        /// <summary>
        /// The renderer
        /// </summary>
        public class Renderer
        {
            /// <summary>The sections.</summary>
            private Editor.Sections sections;

            /// <summary>The args.</summary>
            private RenderContentEditorArgs args;

            /// <summary>The current section.</summary>
            private Editor.Section currentSection;

            /// <summary>The text.</summary>
            private System.Web.UI.HtmlTextWriter text;

            /// <summary>The current field.</summary>
            private Editor.Field currentField;

            /// <summary>The render.</summary>
            /// <param name="arguments">The args.</param>
            public void Render(RenderContentEditorArgs arguments)
            {
                Assert.ArgumentNotNull(arguments, "args");
                if (arguments.Item == null)
                {
                    return;
                }
                GetContentEditorSkinArgs getContentEditorSkinArgs = new GetContentEditorSkinArgs(arguments.Item, arguments.Sections);
                using (new LongRunningOperationWatcher(Settings.Profiling.RenderFieldThreshold, "GetContentEditorSkin pipeline", new string[0]))
                {
                    CorePipeline.Run("getContentEditorSkin", getContentEditorSkinArgs);
                }
                string skin = getContentEditorSkinArgs.Skin;
                if (string.IsNullOrEmpty(skin))
                {
                    return;
                }
                XmlDocument xmlDocument = XmlUtil.LoadXml(skin);
                if (xmlDocument.DocumentElement == null)
                {
                    return;
                }
                this.args = arguments;
                this.sections = arguments.Sections;
                this.Render(xmlDocument);
                arguments.AbortPipeline();
            }

            /// <summary>Renders the specified skin.</summary>
            /// <param name="skin">The skin.</param>
            public void Render(XmlDocument skin)
            {
                Assert.ArgumentNotNull(skin, "skin");
                this.text = new System.Web.UI.HtmlTextWriter(new StringWriter());
                this.RenderElement(skin.DocumentElement);
                this.RenderText();
            }

            /// <summary>Renders the literal.</summary>
            /// <param name="element">The element.</param>
            private void AddText(XmlNode element)
            {
                Assert.ArgumentNotNull(element, "element");
                this.text.WriteBeginTag(element.LocalName);
                if (element.Attributes != null)
                {
                    foreach (XmlAttribute xmlAttribute in element.Attributes)
                    {
                        this.text.WriteAttribute(xmlAttribute.LocalName, xmlAttribute.Value);
                    }
                }
                if (element.HasChildNodes)
                {
                    this.text.Write('>');
                    foreach (XmlNode element2 in element.ChildNodes)
                    {
                        this.RenderElement(element2);
                    }
                    this.text.WriteEndTag(element.LocalName);
                    return;
                }
                if (string.Compare("div", element.LocalName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    this.text.Write('>');
                    this.text.WriteEndTag(element.LocalName);
                    return;
                }
                this.text.Write(" />");
            }

            /// <summary>Renders the element.</summary>
            /// <param name="element">The element.</param>
            private void RenderChildElements(XmlNode element)
            {
                Assert.ArgumentNotNull(element, "element");
                foreach (XmlNode element2 in element.ChildNodes)
                {
                    this.RenderElement(element2);
                }
            }

            /// <summary>Renders the element.</summary>
            /// <param name="element">The element.</param>
            private void RenderElement(XmlNode element)
            {
                Assert.ArgumentNotNull(element, "element");
                if (element.NodeType == XmlNodeType.Comment)
                {
                    return;
                }
                if (element.NodeType == XmlNodeType.Text)
                {
                    this.text.Write(element.Value);
                    return;
                }
                if (element.NodeType == XmlNodeType.CDATA)
                {
                    this.text.Write(element.Value);
                    return;
                }
                if (element.NamespaceURI == "http://www.sitecore.net/skin")
                {
                    this.RenderText();
                    string localName;
                    switch (localName = element.LocalName)
                    {
                        case "label":
                            this.RenderLabel(element);
                            return;
                        case "input":
                            this.RenderInput(element);
                            return;
                        case "fields":
                            this.RenderFields();
                            return;
                        case "section":
                            this.RenderSection(element);
                            return;
                        case "sectionpanel":
                            this.RenderSectionPanel(element);
                            return;
                        case "sections":
                            this.RenderSections();
                            return;
                        case "buttons":
                            this.RenderButtons(element);
                            return;
                        case "marker":
                            this.RenderMarker(element);
                            return;
                    }
                    throw new Exception("Unknown element: " + element.Name);
                }
                this.AddText(element);
            }

            /// <summary>Renders the fields.</summary>
            /// <param name="section">The section.</param>
            private void RenderFields(Editor.Section section)
            {
                Assert.ArgumentNotNull(section, "section");
                foreach (Editor.Field current in section.Fields)
                {
                    this.RenderButtons(current);
                    this.RenderLabel(current);
                    this.RenderInput(current);
                }
            }

            /// <summary>
            /// Renders the section.
            /// </summary>
            private void RenderFields()
            {
                if (this.currentSection != null)
                {
                    this.RenderFields(this.currentSection);
                    return;
                }
                this.RenderSections();
            }

            /// <summary>Renders the field.</summary>
            /// <param name="element">The element.</param>
            private void RenderButtons(XmlNode element)
            {
                Assert.ArgumentNotNull(element, "element");
                string attribute = XmlUtil.GetAttribute("id", element);
                if (string.IsNullOrEmpty(attribute))
                {
                    return;
                }
                Editor.Field fieldByID = this.sections.GetFieldByID(attribute);
                if (fieldByID == null)
                {
                    return;
                }
                this.RenderButtons(fieldByID);
            }

            /// <summary>Renders the field.</summary>
            /// <param name="field">The field.</param>
            private void RenderButtons(Editor.Field field)
            {
                Assert.ArgumentNotNull(field, "field");
                bool readOnly = this.args.ReadOnly;
                Field itemField = field.ItemField;
                Item fieldType = this.args.EditorFormatter.GetFieldType(itemField);
                if (fieldType == null)
                {
                    return;
                }
                if (!itemField.CanWrite)
                {
                    readOnly = true;
                }
                this.args.EditorFormatter.RenderMenuButtons(this.args.Parent, field, fieldType, readOnly);
            }

            /// <summary>Renders the field.</summary>
            /// <param name="field">The field.</param>
            private void RenderInput(Editor.Field field)
            {
                Assert.ArgumentNotNull(field, "field");
                bool readOnly = this.args.ReadOnly;
                Field itemField = field.ItemField;
                Item fieldType = this.args.EditorFormatter.GetFieldType(itemField);
                if (fieldType == null)
                {
                    return;
                }
                if (!itemField.CanWrite)
                {
                    readOnly = true;
                }
                Assert.ArgumentNotNull((object)args, "args");
                args.EditorFormatter = new CustomEditorFormatter() { Arguments = args };
                this.args.EditorFormatter.RenderField(this.args.Parent, field, fieldType, readOnly);
            }

            /// <summary>Renders the field.</summary>
            /// <param name="element">The element.</param>
            private void RenderInput(XmlNode element)
            {
                Assert.ArgumentNotNull(element, "element");
                string attribute = XmlUtil.GetAttribute("section", element);
                if (!string.IsNullOrEmpty(attribute))
                {
                    Editor.Section sectionByName = this.sections.GetSectionByName(attribute);
                    if (sectionByName != null)
                    {
                        this.RenderFields(sectionByName);
                    }
                    return;
                }
                string attribute2 = XmlUtil.GetAttribute("id", element);
                if (string.IsNullOrEmpty(attribute2))
                {
                    return;
                }
                Editor.Field fieldByID = this.sections.GetFieldByID(attribute2);
                if (fieldByID == null)
                {
                    return;
                }
                this.RenderInput(fieldByID);
            }

            /// <summary>Renders the label.</summary>
            /// <param name="element">The element.</param>
            private void RenderLabel(XmlNode element)
            {
                Assert.ArgumentNotNull(element, "element");
                string attribute = XmlUtil.GetAttribute("id", element);
                if (string.IsNullOrEmpty(attribute))
                {
                    return;
                }
                Editor.Field fieldByID = this.sections.GetFieldByID(attribute);
                if (fieldByID == null)
                {
                    return;
                }
                this.RenderLabel(fieldByID);
            }

            /// <summary>Renders the label.</summary>
            /// <param name="field">The field.</param>
            private void RenderLabel(Editor.Field field)
            {
                Assert.ArgumentNotNull(field, "field");
                Field itemField = field.ItemField;
                Item fieldType = this.args.EditorFormatter.GetFieldType(itemField);
                if (fieldType == null)
                {
                    return;
                }
                bool readOnly = this.args.ReadOnly;
                if (!itemField.CanWrite)
                {
                    readOnly = true;
                }
                this.args.EditorFormatter.RenderLabel(this.args.Parent, field, fieldType, readOnly);
            }

            /// <summary>
            /// Renders the literal.
            /// </summary>
            private void RenderText()
            {
                string value = this.text.InnerWriter.ToString();
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }
                this.args.Parent.Controls.Add(new System.Web.UI.LiteralControl(value));
                StringWriter stringWriter = this.text.InnerWriter as StringWriter;
                Assert.IsNotNull(stringWriter, "Internal error");
                stringWriter.GetStringBuilder().Length = 0;
            }

            /// <summary>Renders the section.</summary>
            /// <param name="element">The element.</param>
            private void RenderSection(XmlNode element)
            {
                Assert.ArgumentNotNull(element, "element");
                string attribute = XmlUtil.GetAttribute("name", element);
                if (string.IsNullOrEmpty(attribute))
                {
                    return;
                }
                Editor.Section sectionByName = this.sections.GetSectionByName(attribute);
                if (sectionByName == null)
                {
                    return;
                }
                Editor.Section section = this.currentSection;
                this.currentSection = sectionByName;
                bool renderCollapsedSections = UserOptions.ContentEditor.RenderCollapsedSections;
                this.args.EditorFormatter.RenderSectionBegin(this.args.Parent, sectionByName.ControlID, sectionByName.Name, sectionByName.DisplayName, sectionByName.Icon, sectionByName.IsSectionCollapsed, renderCollapsedSections);
                this.RenderChildElements(element);
                this.args.EditorFormatter.RenderSectionEnd(this.args.Parent, renderCollapsedSections, sectionByName.IsSectionCollapsed);
                this.currentSection = section;
            }

            /// <summary>Renders the section.</summary>
            /// <param name="element">The element.</param>
            private void RenderSectionPanel(XmlNode element)
            {
                Assert.ArgumentNotNull(element, "element");
                string attribute = XmlUtil.GetAttribute("name", element);
                if (string.IsNullOrEmpty(attribute))
                {
                    return;
                }
                Editor.Section sectionByName = this.sections.GetSectionByName(attribute);
                if (sectionByName == null)
                {
                    return;
                }
                Editor.Section section = this.currentSection;
                this.currentSection = sectionByName;
                bool renderCollapsedSections = UserOptions.ContentEditor.RenderCollapsedSections;
                this.args.EditorFormatter.RenderSectionBegin(this.args.Parent, sectionByName.ControlID, sectionByName.Name, sectionByName.DisplayName, sectionByName.Icon, sectionByName.IsSectionCollapsed, renderCollapsedSections);
                this.RenderChildElements(element);
                this.args.EditorFormatter.RenderSectionEnd(this.args.Parent, renderCollapsedSections, sectionByName.IsSectionCollapsed);
                this.currentSection = section;
            }

            /// <summary>Renders the section.</summary>
            /// <param name="element">The element.</param>
            private void RenderMarker(XmlNode element)
            {
                Assert.ArgumentNotNull(element, "element");
                string attribute = XmlUtil.GetAttribute("id", element);
                if (string.IsNullOrEmpty(attribute))
                {
                    return;
                }
                Editor.Field fieldByID = this.sections.GetFieldByID(attribute);
                if (fieldByID == null)
                {
                    return;
                }
                Editor.Field field = this.currentField;
                this.currentField = fieldByID;
                this.args.EditorFormatter.RenderMarkerBegin(this.args.Parent, fieldByID.ControlID);
                this.RenderChildElements(element);
                this.args.EditorFormatter.RenderMarkerEnd(this.args.Parent);
                this.currentField = field;
            }

            /// <summary>Renders the section.</summary>
            /// <param name="section">The section.</param>
            private void RenderSection(Editor.Section section)
            {
                Assert.ArgumentNotNull(section, "section");
                Editor.Section section2 = this.currentSection;
                this.currentSection = section;
                bool renderCollapsedSections = UserOptions.ContentEditor.RenderCollapsedSections;
                this.args.EditorFormatter.RenderSectionBegin(this.args.Parent, section.ControlID, section.Name, section.DisplayName, section.Icon, section.IsSectionCollapsed, renderCollapsedSections);
                if (renderCollapsedSections)
                {
                    this.RenderFields(section);
                }
                this.args.EditorFormatter.RenderSectionEnd(this.args.Parent, renderCollapsedSections, section.IsSectionCollapsed);
                this.currentSection = section2;
            }

            /// <summary>
            /// Renders the sections.
            /// </summary>
            private void RenderSections()
            {
                foreach (Editor.Section current in this.sections)
                {
                    this.RenderSection(current);
                }
            }
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <value>The item.</value>
        [Obsolete]
        public Item Item
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Gets the sections.
        /// </summary>
        /// <value>The sections.</value>
        [Obsolete]
        public Editor.Sections Sections
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Gets the args.
        /// </summary>
        /// <value>The args.</value>
        [Obsolete]
        public RenderContentEditorArgs Args
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Gets or sets the current section.
        /// </summary>
        /// <value>The current section.</value>
        [Obsolete]
        public Editor.Section CurrentSection
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Gets the text.
        /// </summary>
        /// <value>The text.</value>
        [Obsolete]
        public System.Web.UI.HtmlTextWriter Text
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>Gets the field value.</summary>
        /// <param name="args">The arguments.</param>
        /// <contract>
        ///   <requires name="args" condition="none" />
        /// </contract>
        public void Process(RenderContentEditorArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            new RenderSkinedContentEditor.Renderer().Render(args);
        }

        /// <summary>Renders the specified skin.</summary>
        /// <param name="skin">The skin.</param>
        [Obsolete("Use RenderSkinedContentEditor.Renderer.Render(XmlDocument) method instead")]
        public void Render(XmlDocument skin)
        {
            throw new NotSupportedException("Use RenderSkinedContentEditor.Renderer.Render(XmlDocument) method instead");
        }
    }
}