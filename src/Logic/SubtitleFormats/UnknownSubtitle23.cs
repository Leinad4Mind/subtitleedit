﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Nikse.SubtitleEdit.Logic.SubtitleFormats
{
    public class UnknownSubtitle23 : SubtitleFormat
    {
        //1:  01:00:19.04  01:00:21.05
        static readonly Regex RegexTimeCode1 = new Regex(@"^\s*\d+:\s+\d\d:\d\d:\d\d.\d\d\s+\d\d:\d\d:\d\d.\d\d\s*$", RegexOptions.Compiled);

        public override string Extension
        {
            get { return ".rtf"; }
        }

        public override string Name
        {
            get { return "Unknown 23"; }
        }

        public override bool IsTimeBased
        {
            get { return true; }
        }

        public override bool IsMine(List<string> lines, string fileName)
        {
            var subtitle = new Subtitle();
            LoadSubtitle(subtitle, lines, fileName);
            return subtitle.Paragraphs.Count > _errorCount;
        }

        public override string ToText(Subtitle subtitle, string title)
        {
            var sb = new StringBuilder();
            sb.AppendLine(title);
            sb.AppendLine(@"1ab



23/03/2012
03/05/2012
**:**:**.**
01:00:00.00
**:**:**.**
**:**:**.**
01:01:01.12
01:02:30.00
01:02:54.01
**:**:**.**
**:**:**.**
01:19:33.08
");

            int count = 1;
            foreach (Paragraph p in subtitle.Paragraphs)
            {
                sb.AppendLine(string.Format("{0}:  {1}  {2}\r\n{3}", count.ToString(CultureInfo.InvariantCulture).PadLeft(9, ' '), MakeTimeCode(p.StartTime), MakeTimeCode(p.EndTime), p.Text));
                count++;
            }

            var rtBox = new System.Windows.Forms.RichTextBox {Text = sb.ToString().Trim()};
            return rtBox.Rtf;
        }

        private string MakeTimeCode(TimeCode timeCode)
        {
            return timeCode.ToHHMMSSPeriodFF();
        }

        private TimeCode DecodeTimeCode(string timeCode)
        {
            string[] arr = timeCode.Split(":;,.".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            return new TimeCode(int.Parse(arr[0]), int.Parse(arr[1]), int.Parse(arr[2]), FramesToMillisecondsMax999(int.Parse(arr[3])));
        }

        public override void LoadSubtitle(Subtitle subtitle, List<string> lines, string fileName)
        {
            _errorCount = 0;
            var sb = new StringBuilder();
            foreach (string line in lines)
                sb.AppendLine(line);

            string rtf = sb.ToString().Trim();
            if (!rtf.StartsWith("{\\rtf"))
                return;

            var rtBox = new System.Windows.Forms.RichTextBox();
            try
            {
                rtBox.Rtf = rtf;
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message);
                return;
            }

            lines = new List<string>();
            string text = rtBox.Text.Replace("\r\n", "\n");
            foreach (string line in text.Split('\n'))
                lines.Add(line);

            _errorCount = 0;
            Paragraph p = null;
            sb = new StringBuilder();
            foreach (string line in lines)
            {
                string s = line.TrimEnd();
                if (RegexTimeCode1.IsMatch(s))
                {
                    try
                    {
                        if (p != null)
                        {
                            p.Text = sb.ToString().Trim();
                            subtitle.Paragraphs.Add(p);
                        }
                        sb = new StringBuilder();
                        string[] arr = s.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        if (arr.Length == 3)
                            p = new Paragraph(DecodeTimeCode(arr[1]), DecodeTimeCode(arr[2]), string.Empty);
                    }
                    catch
                    {
                        _errorCount++;
                        p = null;
                    }
                }
                else if (p != null && s.Length > 0)
                {
                    sb.AppendLine(s.Trim());
                }
                else if (s.Trim().Length > 0)
                {
                    _errorCount++;
                }
            }
            if (p != null)
            {
                p.Text = sb.ToString().Trim();
                subtitle.Paragraphs.Add(p);
            }

            subtitle.Renumber(1);
        }

    }
}
