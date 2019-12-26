namespace Sitecore.Support.Shell.Applications.ContentEditor
{
    public class DateTime : Date
    {

        public DateTime()
        {
            base.ShowTime = true;
        }

        protected override string GetCurrentDate() =>
            DateUtil.IsoNow;
    }
}