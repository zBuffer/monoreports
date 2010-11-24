// 
// ReportEngine.cs
//  
// Author:
//       Tomasz Kubacki <Tomasz.Kubacki (at) gmail.com>
// 
// Copyright (c) 2010 Tomasz Kubacki 2010
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using MonoReports.Model;
using System.Collections.Generic;
using MonoReports.Model.Controls;
using System.Collections;
using MonoReports.Model.Data;

namespace MonoReports.Model.Engine
{
    public class ReportEngine
    {

        IReportRenderer ReportRenderer;
        Report Report;
        IDataSource source;
        ReportContext ReportContext;
        Page currentPage = null;
        double heightLeftOnCurrentPage = 0;
        double heightUsedOnCurrentPage = 0;
        int currentGroupIndex = -1;
        Section currentSection = null;
        List<SpanInfo> currentSectionSpans = null;
        List<Control> currentSectionOrderedControls = null;
        List<Control> currentSectionControlsBuffer = null;
        List<Control> currentPageFooterSectionControlsBuffer = null;

		public List<Control> CurrentPageFooterSectionControlsBuffer {
			get {
				return this.currentPageFooterSectionControlsBuffer;
			}
			set {
				currentPageFooterSectionControlsBuffer = value;
			}
		}

        List<Line> currentSectionExtendedLines = null;
        double spanCorrection = 0;
        public bool IsSubreport { get; set; }
		
		public Point SubreportLocation { get; set; }
		
        bool dataSourceHasNextRow = true;
        bool stop = false;

        public ReportEngine(Report report, IReportRenderer renderer)
        {
            Report = report;
            source = Report._dataSource;
            if (source == null)
                source = new DummyDataSource();
            ReportRenderer = renderer;
            currentSectionSpans = new List<SpanInfo>();
            currentSectionOrderedControls = new List<Control>();
            currentSectionExtendedLines = new List<Line>();
            currentSectionControlsBuffer = new List<Control>();
            currentPageFooterSectionControlsBuffer = new List<Control>();
            ReportContext = new ReportContext { CurrentPageIndex = 0, DataSource = null, Parameters = new Dictionary<string, string>(), ReportMode = ReportMode.Preview };
            Report.Pages = new List<Page>();
            nextPage();
            selectCurrentSectionByTemplateSection(Report.ReportHeaderSection);

        }

        public void Process()
        {
            while (!ProcessReportPage())
            {
                nextPage();
            }
            onAfterReportProcess();
        }


        Dictionary<string, List<Control>> controlsFromPreviousSectionPage = new Dictionary<string, List<Control>>();


        T selectCurrentSectionByTemplateSection<T>(T s) where T : Section
        {
            T newSection = null;
            if (controlsFromPreviousSectionPage.ContainsKey(s.Name))
            {
                currentSectionOrderedControls = controlsFromPreviousSectionPage[s.Name];
                controlsFromPreviousSectionPage.Remove(s.Name);
                newSection = currentSectionOrderedControls[0] as T;
                currentSectionOrderedControls.RemoveAt(0);
            }
            else
            {
                newSection = s.CreateControl() as T;

                newSection.Format();
                currentSectionOrderedControls = newSection.Controls.OrderBy(ctrl => ctrl.Top).ToList();
            }

            currentSectionSpans.Clear();
            currentSectionExtendedLines.Clear();
            newSection.Location = new Point(s.Location.X, 0);
            currentSection = newSection;




            currentSectionControlsBuffer.Clear();
            return newSection;
        }

        #region old processing details code
        /*
		void processDetails ()
		{
			
			groupColumnIndeces = new List<int> ();
			for (int i = 0; i < Report.Groups.Count; i++) {
				var col = Report.Fields.FirstOrDefault (cl => cl.Name == Report.Groups [i].GroupingFieldName);
				
				if (col != null)
					groupColumnIndeces.Add (Report.Fields.IndexOf (col)); else {
					groupColumnIndeces.Add (-1);
				}
			}
			
			IDataSource dataSource = (source as IDataSource);
			List<string> sorting = new List<string> ();
			
			if (Report.Groups.Count > 0) {
					
				groupCurrentKey = new List<string> ();
				for (int i = 0; i < Report.Groups.Count; i++) {
					sorting.Add (Report.Groups [i].GroupingFieldName);
					groupCurrentKey.Add (String.Empty);
				}
	
			}
			
			if (dataSource == null)
				dataSource = new DummyDataSource ();
			
			dataSource.ApplySort (sorting);
			
				
			while (dataSource.MoveNext ()) {
	
				for (int g = 0; g < Report.Groups.Count; g++) {
					var currentGroup = Report.Groups [g];
					if (!string.IsNullOrEmpty (currentGroup.GroupingFieldName)) {
						string newKey = dataSource.GetValue (currentGroup.GroupingFieldName, String.Empty);
						if (groupCurrentKey [g] != newKey) {	
									
							if (dataSource.CurrentRowIndex > 0) {
								//processGroupFooter(g);
							}
							groupCurrentKey [g] = newKey;
							//processGroupHeader(g);
							
						}
					} else {
						if (dataSource.IsLast) {
							//processGroupFooter(g);
						}
							
						if (dataSource.CurrentRowIndex == 0) {
							//processGroupHeader(g);
						}
					}
				}
				
				//var detailSection = Report.DetailSection. 
				
				//detailSection.Format ();
				
				//processCrossSectionControls(detailSection);
				//add controls
				
			}
				
			for (int i = Report.Groups.Count - 1; i >= 0; i--) {
					//processGroupFooter(i);	 
			}
			
		}
		*/
        #endregion



        public bool ProcessReportPage()
        {
            bool result = false;
            stop = false;

            do
            {

                result = processSectionUpToHeightTreshold(heightLeftOnCurrentPage);

                if (!result && currentSection.KeepTogether)
                    currentSectionControlsBuffer.Clear();
				
				
                addControlsToCurrentPage(heightUsedOnCurrentPage);

                heightLeftOnCurrentPage -= currentSection.Height;
                heightUsedOnCurrentPage += currentSection.Height;

                if (result)
                {
                    nextSection();
                }
                else
                {
                    return false;
                }
            } while (!stop);

            return result;
        }

        /// <summary>
        /// Processes the section up to heightTreshold.
        /// </summary>
        /// <returns>
        ///  returns <c>true</c> if finished processig section and <c>false</c> while not
        /// </returns>
        /// <param name='pageBreakTreshold'>
        /// maxiumum height (starting from current section Location.Y) after which page will break
        /// </param>
        bool processSectionUpToHeightTreshold(double heightTreshold)
        {
            double span = 0;
            double y = 0;
            double maxHeight = 0;
            double marginBottom = 0;
            double maxControlBottom = 0;       
            double tmpSpan = 0;
            bool result = true;
            double realBreak = 0;
            double breakControlMax = 0;
            
            if (currentSectionOrderedControls.Count > 0)
            {
                maxControlBottom = currentSectionOrderedControls.Max(ctrl => ctrl.Bottom);
            }
            marginBottom = currentSection.Height - maxControlBottom;
           
            for (int i = 0; i < currentSectionOrderedControls.Count; i++)
            {

                var control = currentSectionOrderedControls[i];
                tmpSpan = 0;
              
                if (!control.IsVisible)
                    continue;

                if (control is Line && (control as Line).ExtendToBottom)
                {
					var line = control as Line;
                    currentSectionExtendedLines.Add(line);					
                }

                if (source != null)
                    control.AssignValue(source);


                y = control.Top + span;
                Size controlSize = ReportRenderer.MeasureControl(control);

				if(control is SubReport){
					SubReport sr = control as SubReport;
					sr.ProcessUpToPage(this.ReportRenderer,heightTreshold);
					currentSectionOrderedControls.AddRange(sr.Engine.currentSectionOrderedControls);
				}

                foreach (SpanInfo item in currentSectionSpans)
                {
                    if (y > item.Treshold)
                    {
                        tmpSpan = Math.Max(tmpSpan, item.Span);
                    }
                }

                span = tmpSpan;
                control.Top += span;
                double bottomBeforeGrow = control.Bottom;
                control.Size = controlSize;


                if (control.Bottom  <= heightTreshold)
                {
                    currentSectionControlsBuffer.Add(control);
                }
                else
                {
                       
                        result = false;
                        if (!currentSection.KeepTogether)
                        {
                           
                            breakControlMax = control.Height - ((control.Top + control.Height) - heightTreshold);
                            if (realBreak == 0)
                                realBreak = heightTreshold;

                            if (control.Top > heightTreshold)
                            {
                                storeSectionForNextPage();
                                var controlToStore = control.CreateControl();
                                controlToStore.Top -= realBreak;
                                controlsFromPreviousSectionPage[currentSection.Name].Add(controlToStore);
                                sectionToStore.Height = Math.Max(sectionToStore.Height, controlToStore.Bottom + marginBottom);
                                continue;
                            }

                            storeSectionForNextPage();
                            Control[] brokenControl = ReportRenderer.BreakOffControlAtMostAtHeight(control, breakControlMax);
                            realBreak = heightTreshold - (breakControlMax - brokenControl[0].Height);
                            controlsFromPreviousSectionPage[currentSection.Name].Add(brokenControl[1]);
                            currentSectionControlsBuffer.Add(brokenControl[0]);
                            sectionToStore.Height = Math.Max(sectionToStore.Height, brokenControl[1].Bottom + marginBottom);
                        }
                        else
                        {
                            currentSectionControlsBuffer.Add(control);
                        }
                    
                }

                if (currentSection.CanGrow && maxHeight <= control.Bottom)
                {
                    maxHeight = control.Bottom;
                }

                currentSectionSpans.Add(
                new SpanInfo
                {
                    Treshold = bottomBeforeGrow,
                    Span = span + control.Bottom - bottomBeforeGrow
                });
            }


            var heighWithMargin = maxHeight + marginBottom;

            if (heightTreshold < heighWithMargin)
            {
                result = false;
            }

            if (!currentSection.CanGrow && !currentSection.CanShrink || !currentSection.CanShrink && heighWithMargin < currentSection.Height)
            {
                ;
            }
            else if (heighWithMargin <= heightTreshold)
            {
                currentSection.Height = heighWithMargin;
            }           
            
            foreach (Line lineItem in currentSectionExtendedLines)
            {
                if (lineItem.Location.Y == lineItem.End.Y)
                {
                    lineItem.Location = new Point(lineItem.Location.X, maxControlBottom + marginBottom - lineItem.LineWidth / 2);
                    lineItem.End = new Point(lineItem.End.X, maxControlBottom + marginBottom - lineItem.LineWidth / 2);
                }
                else if (lineItem.Location.Y > lineItem.End.Y)
                {
                    lineItem.Location = new Point(lineItem.Location.X, Math.Min( heighWithMargin , heightTreshold ));
                }
                else
                {
                    lineItem.End = new Point(lineItem.End.X,  Math.Min( heighWithMargin , heightTreshold ));
                }
				
				if (heighWithMargin > heightTreshold) {
					
					
					
					var newCtrl =  lineItem.CreateControl();
					
					if(lineItem.Location.Y == lineItem.End.Y)
						lineItem.IsVisible = false;					
					newCtrl.Top = 0;
					storeSectionForNextPage();
					controlsFromPreviousSectionPage[currentSection.Name].Add(newCtrl);
				}
            }

            sectionToStore = null;
            if (!currentSection.CanGrow)
            {
                controlsFromPreviousSectionPage.Remove(currentSection.Name);
                result = true;
            }
           return result;
        }

        Section sectionToStore = null;

        void storeSectionForNextPage()
        {

            if (!controlsFromPreviousSectionPage.ContainsKey(currentSection.Name))
            {
                sectionToStore = currentSection.CreateControl() as Section;

                var controlsToNextPage = new List<Control>();
                controlsToNextPage.Add(sectionToStore);
                controlsFromPreviousSectionPage.Add(currentSection.Name, controlsToNextPage);
                sectionToStore.Height = 0;
            }
        }


        void nextRecord()
        {
            dataSourceHasNextRow = source.MoveNext();
        }

        void nextSection()
        {

            switch (currentSection.SectionType)
            {

                case SectionType.ReportHeader:
                    if (Report.ReportHeaderSection.BreakPageAfter)
                        nextPage();
                    nextRecord();
                    selectCurrentSectionByTemplateSection(Report.PageHeaderSection);
                    break;
                case SectionType.PageHeader:

                    selectCurrentSectionByTemplateSection(Report.PageFooterSection);
                    break;
                case SectionType.PageFooter:

                    if (Report.Groups.Count > 0)
                    {
                        currentGroupIndex = 0;
                        selectCurrentSectionByTemplateSection(Report.GroupHeaderSections[currentGroupIndex]);
                    }
                    else
                    {
                        if (dataSourceHasNextRow)
                            selectCurrentSectionByTemplateSection(Report.DetailSection);
                        else
                            selectCurrentSectionByTemplateSection(Report.ReportFooterSection);
                    }
                    break;
                case SectionType.GroupHeader:

                    if (currentGroupIndex < Report.Groups.Count - 1)
                    {
                        currentGroupIndex++;
                        selectCurrentSectionByTemplateSection(Report.GroupHeaderSections[currentGroupIndex]);
                    }
                    else
                    {
                        if (dataSourceHasNextRow)
                            selectCurrentSectionByTemplateSection(Report.DetailSection);
                        else
                            selectCurrentSectionByTemplateSection(Report.ReportFooterSection);
                    }
                    break;

                case SectionType.Details:
                    nextRecord();
                    if (dataSourceHasNextRow)
                    {
                        selectCurrentSectionByTemplateSection(Report.DetailSection);
                    }
                    else
                    {
                        selectCurrentSectionByTemplateSection(Report.ReportFooterSection);
                    }
                    break;

                case SectionType.GroupFooter:


                    break;

                case SectionType.ReportFooter:
                    addControlsToCurrentPage(Report.Height - Report.PageFooterSection.Height, currentPageFooterSectionControlsBuffer);
                    stop = true;
                    break;
                default:
                    break;
            }

            if (!currentSection.IsVisible)
                nextSection();
        }

        void addControlsToCurrentPage(double span)
        {
            if (currentSection.SectionType != SectionType.PageFooter)
            {
                addControlsToCurrentPage(span + spanCorrection, currentSectionControlsBuffer);
            }
            else
            {
                currentPageFooterSectionControlsBuffer.AddRange(currentSectionControlsBuffer);
                spanCorrection -= currentSection.Height;
            }
            currentSectionControlsBuffer.Clear();
        }
		
		
		

        void addControlsToCurrentPage(double span, List<Control> controls)
        {
            foreach (var control in controls)
            {
                control.Top += span;				
				if (IsSubreport) {
					control.Top += SubreportLocation.Y;
					control.Left += SubreportLocation.X;
				}
                currentPage.Controls.Add(control);
            }
        }

        void nextPage()
        {
            addControlsToCurrentPage(Report.Height - Report.PageFooterSection.Height, currentPageFooterSectionControlsBuffer);
            spanCorrection = 0;
            ReportContext.CurrentPageIndex++;
            currentPage = new Page { PageNumber = ReportContext.CurrentPageIndex };
            heightLeftOnCurrentPage = Report.Height;
            heightUsedOnCurrentPage = 0;
            currentPageFooterSectionControlsBuffer.Clear();
            Report.Pages.Add(currentPage);
            selectCurrentSectionByTemplateSection(Report.PageHeaderSection);
        }

        private void onAfterReportProcess()
        {
            //todo exec Report event

        }

    }

    internal struct SpanInfo
    {
        internal double Treshold;
        internal double Span;
    }

    static internal class SectionExtensions
    {

        public static IEnumerable<Control> GetCrossSectionControls(this Section section, Section endSection)
        {

            foreach (var c in section.Controls.Where(ctrl => ctrl is ICrossSectionControl))
            {

                ICrossSectionControl csc = c as ICrossSectionControl;
                csc.StartSection = section;
                csc.EndSection = endSection;
                yield return c;
            }
        }

    }
}

