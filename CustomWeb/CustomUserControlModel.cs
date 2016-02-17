using System;
using System.Web;

namespace Nts.DataHelper.CustomWeb
{
    public abstract class CustomUserControlModel<T> : CustomUserControl where T : new()
    {
        
        protected void RedirectToModelId(int idModel)
        {
            Response.Redirect(Request.Url.GetLeftPart(UriPartial.Path) + "?" + typeof(T).GetIdentityName() + "=" + idModel);
        }

        public void ReloadModel()
        {
            _model = DataHelper.Load<T>(IdModel);
        }

        private int ? _modelId;
        
        public int IdModel
        {
            get
            {
                if (!_modelId.HasValue)
                {
                    _modelId = string.IsNullOrEmpty(Request.QueryString[typeof(T).GetIdentityName()]) ? 0 : int.Parse(Request.QueryString[typeof(T).GetIdentityName()]);
                }
                return _modelId.Value;
            }
            set
            {
                _modelId = value;
                _model = DataHelper.Load<T>(IdModel);
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
        public abstract override void ForceLoad();
    }
}