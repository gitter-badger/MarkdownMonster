﻿using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace MarkdownMonster
{
    /// <summary>
    /// Class that wraps the Active Markdown document used in the
    /// editor.
    /// [ComVisible] is important as we access this from JavaScript
    /// </summary>
    [ComVisible(true)]
    public class MarkdownDocument : INotifyPropertyChanged
    {
        /// <summary>
        ///  Name of the Markdown file
        /// </summary>
        public string Filename
        {
            get { return _filename; }
            set
            {
                if (value == _filename) return;
                _filename = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilenameWithIndicator));
                OnPropertyChanged(nameof(HtmlRenderFilename));
            }
        }

        public int LastBrowserScrollPosition { get; set; }

        public MarkdownStyles MarkdownStyle = MarkdownStyles.GitHub;

        public string FilenameWithIndicator
        {
            get
            {
                return Path.GetFileName(Filename)  + (IsDirty ? "*" : "");
            }
        }


        /// <summary>
        /// Determines whether the active document has changes
        /// that have not been saved yet
        /// </summary>
        [JsonIgnore]
        public bool IsDirty
        {
            get { return _IsDirty; }
            set
            {
                if (value != _IsDirty)
                {
                    _IsDirty = value;
                    IsDirtyChanged?.Invoke(value);
                    OnPropertyChanged(nameof(IsDirty));
                    OnPropertyChanged(nameof(FilenameWithIndicator));
                }
            }
        }
        private bool _IsDirty;
        private string _filename;

        /// <summary>
        /// This is the filename used when rendering this document to HTML
        /// </summary>
        [JsonIgnore]
        public string HtmlRenderFilename
        {
            get
            {
                string path = null;
                string file = null;

                if (Filename == "untitled")
                {
                    path = Path.GetDirectoryName(mmApp.Configuration.CommonFolder);
                    file = "__untitled.htm";
                }
                else
                {
                    path = Path.GetDirectoryName(Filename);
                    file = "__" + Path.ChangeExtension(Path.GetFileName(Filename), "htm");                    
                }
                return Path.Combine(path, file);
            }
        }

        /// <summary>
        /// Event fired when 
        /// </summary>
        public event Action<bool> IsDirtyChanged;

        /// <summary>
        /// Holds the actively edited Markdown text
        /// </summary>
        [JsonIgnore]
        public string CurrentText { get; set; }


        /// <summary>
        /// Loads the markdown document into the CurrentText
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool Load(string filename = null)
        {
            if (string.IsNullOrEmpty(filename))
                filename = Filename;

            if (!File.Exists(filename))
                return false;

            CurrentText = File.ReadAllText(filename);

            return true;
        }
        
        /// <summary>
        /// Saves the CurrentText into the specified filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool Save(string filename = null)
        {
            if (string.IsNullOrEmpty(filename))
                filename = Filename;

            File.WriteAllText(filename, CurrentText);
            IsDirty = false;

            return true;
        }

        /// <summary>
        /// Cleans up after the file is closed by deleting
        /// the HTML render file.
        /// </summary>
        public void Close()
        {
            if (File.Exists(HtmlRenderFilename))
                File.Delete(HtmlRenderFilename);
        }
        
        /// <summary>
        /// Renders markdown of the current document text into HTML
        /// </summary>
        /// <param name="markdown"></param>
        /// <returns></returns>
        public string RenderHtml(string markdown = null)
        {
            if (string.IsNullOrEmpty(markdown))
                markdown = CurrentText;

            var parser = MarkdownParser.GetParser(MarkdownStyle);

            return parser.Parse(markdown);
        }

        public string RenderHtmlToFile(string markdown = null, string filename = null)
        {
            string markdownHtml = RenderHtml(markdown);

            if (string.IsNullOrEmpty(filename))
                filename = HtmlRenderFilename;

            var themePath = Path.Combine(Environment.CurrentDirectory, "PreviewThemes\\" + 
                mmApp.Configuration.RenderTheme + "\\");
            var themeHtml = File.ReadAllText(themePath + "theme.html");

            var html = themeHtml.Replace("{$themePath}", themePath);
            html = html.Replace("{$markdownHtml}",markdownHtml);

            File.WriteAllText(filename, html);
            return html;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        ~MarkdownDocument()
        {
            this.Close();
        }
    }
}