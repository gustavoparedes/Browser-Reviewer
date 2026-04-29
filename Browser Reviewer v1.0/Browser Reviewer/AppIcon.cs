using System.Drawing;

namespace Browser_Reviewer
{
    internal static class AppIcon
    {
        public static void Apply(Form form)
        {
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Browser Reviewer.ico");
                if (File.Exists(iconPath))
                    form.Icon = new Icon(iconPath);
            }
            catch
            {
                // Keep the designer icon if the runtime icon cannot be loaded.
            }
        }
    }
}
