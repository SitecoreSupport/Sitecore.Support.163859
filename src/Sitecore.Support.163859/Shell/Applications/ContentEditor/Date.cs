using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using System;

namespace Sitecore.Support.Shell.Applications.ContentEditor
{
    public class Date : Input, IContentField
    {
        private DateTimePicker picker;

        public Date()
        {
            this.Initialize();
        }

        private void ClearField()
        {
            this.SetRealValue(string.Empty);
        }

        protected virtual string GetCurrentDate() =>
            DateUtil.ToIsoDate(DateUtil.ToServerTime(System.DateTime.UtcNow).Date);

        protected override Item GetItem() =>
            Client.ContentDatabase.GetItem(this.ItemID);

        public string GetValue()
        {
            string isoDate = (this.picker != null) ? (this.IsModified ? this.picker.Value : this.RealValue) : this.RealValue;
            return (isoDate.StartsWith("$", StringComparison.InvariantCulture) ? isoDate : DateUtil.IsoDateToUtcIsoDate(isoDate));
        }

        public override void HandleMessage(Message message)
        {
            string str;
            Assert.ArgumentNotNull(message, "message");
            base.HandleMessage(message);
            if ((message["id"] == this.ID) && ((str = message.Name) != null))
            {
                if (str == "contentdate:today")
                {
                    this.Today();
                }
                else if (str == "contentdate:clear")
                {
                    this.ClearField();
                }
            }
        }

        protected virtual void Initialize()
        {
            this.Class = "scContentControl";
            base.Change = "#";
            base.Activation = true;
            this.ShowTime = false;
        }

        protected override bool LoadPostData(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                value = DateUtil.IsoDateToServerTimeIsoDate(value);
            }
            if (!base.LoadPostData(value))
            {
                return false;
            }
            this.picker.Value = value ?? string.Empty;
            return true;
        }

        protected override void OnInit(EventArgs e)
        {
            this.picker = new DateTimePicker();
            this.picker.ID = this.ID + "_picker";
            this.Controls.Add(this.picker);
            if (!string.IsNullOrEmpty(this.RealValue))
            {
                this.picker.Value = this.RealValue;
            }
            this.picker.Changed += (param0, param1) => this.SetModified();
            this.picker.ShowTime = this.ShowTime;

            //fixing bug #163859
            var item = GetItem();
            if (item != null && !item.Locking.HasLock() && !Sitecore.Context.IsAdministrator)
            {
                this.Disabled = true;
            }
            //end of the fix
            this.picker.Disabled = this.Disabled;
            base.OnInit(e);
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            base.ServerProperties["Value"] = base.ServerProperties["Value"];
            base.ServerProperties["RealValue"] = base.ServerProperties["RealValue"];
        }

        protected override void SetModified()
        {
            base.SetModified();
            this.IsModified = true;
            if (base.TrackModified)
            {
                Sitecore.Context.ClientPage.Modified = true;
            }
        }

        protected void SetRealValue(string realvalue)
        {
            realvalue = DateUtil.IsoDateToServerTimeIsoDate(realvalue);
            if (realvalue != this.RealValue)
            {
                this.SetModified();
            }
            this.RealValue = realvalue;
            this.picker.Value = realvalue;
        }

        public void SetValue(string value)
        {
            Assert.ArgumentNotNull(value, "value");
            this.Value = value;
            value = value.StartsWith("$", StringComparison.InvariantCulture) ? value : DateUtil.IsoDateToServerTimeIsoDate(value);
            this.RealValue = value;
            if (this.picker != null)
            {
                this.picker.Value = value;
            }
        }

        private void Today()
        {
            this.SetRealValue(this.GetCurrentDate());
        }

        public string ItemID
        {
            get =>
                base.GetViewStateString("ItemID");
            set
            {
                Assert.ArgumentNotNull(value, "value");
                base.SetViewStateString("ItemID", value);
            }
        }

        public string RealValue
        {
            get =>
                base.GetViewStateString("RealValue");
            set
            {
                Assert.ArgumentNotNull(value, "value");
                string str = value.StartsWith("$", StringComparison.InvariantCulture) ? value : DateUtil.IsoDateToServerTimeIsoDate(value);
                base.SetViewStateString("RealValue", str);
            }
        }

        public bool ShowTime
        {
            get =>
                base.GetViewStateBool("Showtime", false);
            set =>
                base.SetViewStateBool("Showtime", value);
        }

        public bool IsModified
        {
            get =>
                System.Convert.ToBoolean(base.ServerProperties["IsModified"]);
            protected set =>
                base.ServerProperties["IsModified"] = value;
        }
    }
}