using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace CSharpKit {
	public static class MvcMultipleSectionExtension {
		private class SectionRenderer : ISectionRenderer {
			private class Item {
				public Func<WebViewPage, IHtmlString> content;
				public WebViewPage page;
			}

			private List<Item> items = new List<Item>();

			internal void Add(WebViewPage page, Func<WebViewPage, IHtmlString> content) {
				items.Add(new Item { page = page, content = content });
			}

			public void Clear() {
				items.Clear();
			}

			public HtmlString Render() {
				return this.Render(true);
			}

			public HtmlString Render(bool clear) {
				System.Text.StringBuilder buffer = new System.Text.StringBuilder();

				foreach (Item item in items)
					buffer.Append(item.content(item.page).ToHtmlString());

				if (clear)
					this.Clear();

				return new HtmlString(buffer.ToString());
			}
		}

        private class EmptySectionRenderer : ISectionRenderer {

            public void Clear()
            {
                // Nada a fazer
            }

            public HtmlString Render()
            {
                return new HtmlString(string.Empty);
            }

            public HtmlString Render(bool clear)
            {
                return new HtmlString(string.Empty);
            }
        }

		public interface ISectionRenderer {
			void Clear();

			HtmlString Render();
			HtmlString Render(bool clear);
		}

		private static Dictionary<HttpContext, Dictionary<string, SectionRenderer>> globalSections = new Dictionary<HttpContext, Dictionary<string, SectionRenderer>>();
        public static readonly ISectionRenderer EmptyRenderer = new EmptySectionRenderer();

		private class RemoveContext : IDisposable {
			private HttpContext context;

			public RemoveContext(HttpContext context) {
				this.context = context;
			}

			public void Dispose() {
				lock (globalSections)
					globalSections.Remove(context);
			}
		}

		private static SectionRenderer GetSection(string section, bool create = false) {
			Dictionary<string, SectionRenderer> sections;
			lock (globalSections) {
				HttpContext httpContext = HttpContext.Current;

				if (!globalSections.TryGetValue(httpContext, out sections)) {
					sections = new Dictionary<string, SectionRenderer>();

					globalSections.Add(httpContext, sections);
					httpContext.DisposeOnPipelineCompleted(new RemoveContext(httpContext));
				}
			}

			if (string.IsNullOrEmpty(section))
				section = string.Empty;

			SectionRenderer sectionObj;
            if (!sections.TryGetValue(section, out sectionObj))
                if (create)
                {
                    sectionObj = new SectionRenderer();
                    sections.Add(section, sectionObj);
                }
                else
                    sectionObj = null;

			return sectionObj;
		}

		public static ISectionRenderer Section(this WebViewPage page, string section) {
            return page.Section(section, true);
		}

        public static ISectionRenderer Section(this WebViewPage page, string section, bool required)
        {
            ISectionRenderer sectionObj = GetSection(section);

            if (sectionObj == null)
                if (required)
                    throw new ArgumentException();
                else
                    return EmptyRenderer;

            return sectionObj;
        }

		public static void Section(this WebViewPage page, string section, Func<WebViewPage, IHtmlString> content) {
			GetSection(section, true).Add(page, content);
		}
	}
}
