// 
// Control.cs
//  
// Author:
//       Tomasz Kubacki <Tomasz.Kubacki(at)gmail.com>
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
using System.Drawing;
using MonoReports.Model;
using MonoReports.Model.Data;

namespace MonoReports.Model.Controls
{

	public abstract class Control  : ICloneable
	{

		public Control ()
		{
			BackgroundColor = System.Drawing.Color.Transparent;
			Location = new Point(0,0);
			Size =  new Size(0,0);
			IsVisible = true;
		}
		
		public Control TemplateControl {get;set;}

		public Point Location { get; set; }

		public Size Size { get; set; }
		
		public Color BackgroundColor {get;set;}

		public double Height {

			get { return Size.Height; }
		}


		public double Width {

			get { return Size.Width; }
		}
			

		public double Top {

			get { return Location.Y; }
		}

		public double Left {

			get { return Location.X; }
		}
		
		public bool CanGrow {get;set;}
		
		public bool CanShrink {get;set;}

		public bool IsVisible { get; set; }
		
		 
		internal double measureBottomMarginFromSection(Section s){
			
			 return  s.Height - (Location.Y +  Size.Height);
		 
		}
		
		public virtual void MoveControlByY(double y){
			Location = new Point(this.Location.X,this.Location.Y + y);
		}
		
		public abstract object Clone ();
		
		
		internal void CopyBasicProperties(Control c){
			c.Location = new Point(Location.X,Location.Y);
			c.Size = new Size(Size.Width,Size.Height);
			c.CanGrow = CanGrow;
			c.CanShrink = CanShrink;
			c.IsVisible = IsVisible;
			c.BackgroundColor = Color.FromArgb(BackgroundColor.ToArgb());
			c.TemplateControl = this;
			
		}
		
		public virtual void AssignValue(Data.IDataSource source, DataRow row){
		}
		
	}
}
