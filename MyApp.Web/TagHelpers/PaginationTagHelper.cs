using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using MyApp.Web.Common;

namespace MyApp.Web.TagHelpers;

[HtmlTargetElement("pagination")]
public class PaginationTagHelper : TagHelper
{
    [HtmlAttributeName("meta")]
    public PaginationMeta? Meta { get; set; }

    [HtmlAttributeName("url")]
    public string Url { get; set; } = string.Empty;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "nav";
        output.Attributes.SetAttribute("aria-label", "Page navigation");

        if (Meta == null || Meta.TotalPages <= 1)
        {
            output.SuppressOutput();
            return;
        }

        var ul = new TagBuilder("ul");
        ul.AddCssClass("pagination justify-content-end mb-0");

        // Previous
        var prevLi = new TagBuilder("li");
        prevLi.AddCssClass("page-item" + (Meta.HasPrevious ? "" : " disabled"));
        prevLi.InnerHtml.AppendHtml($"<a class=\"page-link\" href=\"{Url}?pageNumber={Meta.PageNumber - 1}&pageSize={Meta.PageSize}\">&laquo;</a>");
        ul.InnerHtml.AppendHtml(prevLi);

        // Pages
        for (int i = 1; i <= Meta.TotalPages; i++)
        {
            var li = new TagBuilder("li");
            li.AddCssClass("page-item" + (i == Meta.PageNumber ? " active" : ""));
            li.InnerHtml.AppendHtml($"<a class=\"page-link\" href=\"{Url}?pageNumber={i}&pageSize={Meta.PageSize}\">{i}</a>");
            ul.InnerHtml.AppendHtml(li);
        }

        // Next
        var nextLi = new TagBuilder("li");
        nextLi.AddCssClass("page-item" + (Meta.HasNext ? "" : " disabled"));
        nextLi.InnerHtml.AppendHtml($"<a class=\"page-link\" href=\"{Url}?pageNumber={Meta.PageNumber + 1}&pageSize={Meta.PageSize}\">&raquo;</a>");
        ul.InnerHtml.AppendHtml(nextLi);

        output.Content.AppendHtml(ul);
    }
}
