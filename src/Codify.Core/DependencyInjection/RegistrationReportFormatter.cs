using Codify.Core.DependencyInjection.Models;
using System.Linq;
using System.Text;

namespace Codify.Core.DependencyInjection
{
    public static class RegistrationReportFormatter
    {
        public static string Format(RegistrationReport report)
        {
            var sb = new StringBuilder();

            foreach (var module in report.Items.GroupBy(x => x.Module))
            {
                sb.AppendLine(module.Key);
                sb.AppendLine(new string('─', 40));

                foreach (var item in module)
                {
                    sb.Append("✓ ");

                    sb.Append(item.Service.Name);

                    if (item.Service != item.Implementation)
                    {
                        sb.AppendLine();
                        sb.Append("    → ");
                        sb.Append(item.Implementation.Name);
                    }

                    sb.AppendLine();
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}
