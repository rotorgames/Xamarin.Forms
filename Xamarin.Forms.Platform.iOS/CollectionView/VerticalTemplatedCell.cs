using System;
using System.Diagnostics;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Xamarin.Forms.Platform.iOS
{
	internal sealed class VerticalTemplatedCell : TemplatedCell
	{
		public static NSString ReuseId = new NSString("Xamarin.Forms.Platform.iOS.VerticalTemplatedCell");

		[Export("initWithFrame:")]
		public VerticalTemplatedCell(CGRect frame) : base(frame)
		{
		}

		public override CGSize Measure()
		{
			var measure = VisualElementRenderer.Element.Measure(ConstrainedDimension, 
				double.PositiveInfinity, MeasureFlags.IncludeMargins);

			var height = measure.Request.Height;

			VisualElementRenderer.Element.Layout(new Rectangle(Point.Zero, measure.Request));

			return new CGSize(ConstrainedDimension, height);
		}

		public override void ConstrainTo(CGSize constraint)
		{
			ConstrainedDimension = constraint.Width;
			Layout(constraint);
		}
	}
}