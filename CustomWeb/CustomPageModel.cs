// -----------------------------------------------------------------------
// <copyright file="CustomPageModel.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Web;

namespace Nts.DataHelper.CustomWeb
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class CustomPageModel<T> : CustomPage where T : new()
    {
        protected void RedirectToModelId(int idModel)
        {
            Response.Redirect(Request.Url.GetLeftPart(UriPartial.Path) + "?" + typeof(T).GetIdentityName() + "=" + idModel);
        }
        public int IdModel
        {
            get
            {
                var identityName = typeof(T).GetIdentityName();
                if (string.IsNullOrEmpty(Request.QueryString[identityName]))
                    return 0;
                return int.Parse(Request.QueryString[typeof(T).GetIdentityName()]);
            }
        }
        public static string BaseSiteUrl
        {
            get
            {
                HttpContext context = HttpContext.Current;
                string baseUrl = context.Request.Url.Scheme + "://" + context.Request.Url.Authority;// +context.Request.ApplicationPath.TrimEnd('/');
                return baseUrl;
            }
        }
        private T _model;
        public T Model
        {
            get
            {
                if (_model == null)
                    _model = DataHelper.Load<T>(IdModel);
                if (_model == null)
                    _model = new T();
                return _model;
            }
        }

    }
}
