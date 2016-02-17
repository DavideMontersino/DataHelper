// -----------------------------------------------------------------------
// <copyright file="CustomPage.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System.Web.UI;

namespace Nts.DataHelper.CustomWeb
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class CustomPage : Page
    {
        public void Reload()
        {
            Response.Redirect(Request.RawUrl);
        }
        
    }
}
