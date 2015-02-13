using System.Web;
using System.Web.Optimization;

namespace MBHelper
{
    public class BundleConfig
    {
        // For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
        public static void RegisterBundles(BundleCollection bundles)
        {
            //bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
            //            "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryui").Include(
                        "~/Scripts/jquery-ui-1.10.2.min.js"));
   
            bundles.Add(new ScriptBundle("~/bundles/myscripts").Include(
                        "~/Scripts/DataTables-1.9.4/media/js/jquery.dataTables.js",  
                        "~/Scripts/jquery.numeric.js",
                        "~/Scripts/jQDateRangeSlider-min.js",
                        "~/Scripts/date.format.js",
                        "~/Scripts/jquery.multiselect.js",
                        "~/Scripts/Odds.js"));


            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                        "~/Content/bootstrap/js/bootstrap.js"));           
             

            bundles.Add(new StyleBundle("~/Content/css").Include(
                        "~/Content/site.css",
                        "~/Content/odds.css",
                        "~/Content/jquery.multiselect.css",
                        "~/Content/classic.css"));


            bundles.Add(new StyleBundle("~/Content/DataTables-1.9.4/media/css/bundle").Include(
                        "~/Content/DataTables-1.9.4/media/css/demo_table.css"));  

            bundles.Add(new StyleBundle("~/Content/bootstrap/css/bundle").Include(
                        "~/Content/bootstrap/css/bootstrap.css"));  

            bundles.Add(new StyleBundle("~/Content/themes/custom-theme/css").Include(
                        "~/Content/themes/custom-theme/jquery-ui-1.10.0.custom.css"));

            bundles.Add(new StyleBundle("~/Content/themes/base/css").Include(
                       "~/Content/themes/base/jquery.ui.core.css"
                       //"~/Content/themes/base/jquery.ui.resizable.css",
                       //"~/Content/themes/base/jquery.ui.selectable.css"
                       //"~/Content/themes/base/jquery.ui.accordion.css",
                       //"~/Content/themes/base/jquery.ui.autocomplete.css",
                       //"~/Content/themes/base/jquery.ui.button.css",
                       //"~/Content/themes/base/jquery.ui.dialog.css",
                       //"~/Content/themes/base/jquery.ui.slider.css",
                       //"~/Content/themes/base/jquery.ui.tabs.css",
                       //"~/Content/themes/base/jquery.ui.datepicker.css",
                       //"~/Content/themes/base/jquery.ui.progressbar.css"
                       ));
        }
    }
}