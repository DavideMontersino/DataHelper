using System.Web.UI;

namespace Nts.DataHelper.CustomWeb
{
    public abstract class CustomUserControl : UserControl
    {
        public void Reload()
        {
            Response.Redirect(Request.RawUrl);
        }

        
       
        public abstract void ForceLoad();

        
    }

}