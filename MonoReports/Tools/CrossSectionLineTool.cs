// 
// CrossSectionLineTool.cs
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
using MonoReports.ControlView;
using MonoReports.Model.Controls;
using MonoReports.Extensions.CairoExtensions;
using Cairo;
namespace MonoReports.Tools
{
	public class CrossSectionLineTool : LineTool
	{
		public CrossSectionLineTool (DesignView designView) : base(designView)
		{
		}


		public override void CreateNewControl (SectionView sectionView)
		{
			if (sectionView.AllowCrossSectionControl) {
				var startPoint = sectionView.PointInSectionByAbsolutePoint (designView.StartPressPoint.X, designView.StartPressPoint.Y);
				var endPoint = new MonoReports.Model.Controls.Point(0, 10);
				var l = new CrossSectionLine { Location = new MonoReports.Model.Controls.Point (startPoint.X, startPoint.Y), End = endPoint };
				
				var lineView = sectionView.AddControl (l) as CrossSectionLineView;
			
				sectionView.Section.Controls.Add (l);
				lineView.EndSection.AddControlView(lineView);
				lineView.ParentSection = sectionView;
				designView.SelectedControl = lineView;
				designView.CrossSectionControls.Add(lineView);
			}
		}
		
		public override void OnAfterDraw (Context c)
		{
			 
			if (designView != null && designView.SelectedControl != null && designView.IsDesign) {
				var p1 = designView
					.SelectedControl
					.ParentSection
					.AbsolutePointByLocalPoint(line.Location.X ,line.Location.Y );
				

				c.DrawGripper (p1);
			}
		}
		
		public override void OnBeforeDraw (Context c)
		{
			
			if (designView.IsPressed) {
				
				if (designView.IsMoving && designView.SelectedControl != null) {
														
					if (startPointHit) {
						line.Location = new MonoReports.Model.Controls.Point ( Math.Max(0, line.Location.X +  designView.DeltaPoint.X), Math.Max(0, line.Location.Y + designView.DeltaPoint.Y));
						
					} else if (endPointHit) {
						
					} else {
						line.Location = new MonoReports.Model.Controls.Point ( Math.Max(0, line.Location.X +  designView.DeltaPoint.X), Math.Max(0, line.Location.Y));
						
					}

				}
				
			}
		}
		
		public override void OnMouseDown ()
		{
			currentSection = designView.SelectedControl != null ? designView.SelectedControl.ParentSection : null;
			line = designView.SelectedControl.ControlModel as Line;
			var location = line.Location;
 			var startPoint = currentSection.PointInSectionByAbsolutePoint(designView.StartPressPoint);
			
			Cairo.PointD startDistance = new Cairo.PointD ( location.X - startPoint.X,  location.Y - startPoint.Y);
			
			if (startDistance.X < 6 && startDistance.X > -6 && startDistance.Y < 6 && startDistance.Y > -6) {
				startPointHit = true;
			}
		}
		

		#region implemented abstract members of MonoReports.Tools.BaseTool
		public override string Name {
			get { return "CrossSectionLineTool"; }
		}
		
		#endregion
		
		
		
		
		
	}
}
