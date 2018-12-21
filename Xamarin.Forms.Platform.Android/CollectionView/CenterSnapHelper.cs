using Android.Support.V7.Widget;

namespace Xamarin.Forms.Platform.Android
{
	public class CenterSnapHelper : LinearSnapHelper
	{
		public override global::Android.Views.View FindSnapView(RecyclerView.LayoutManager layoutManager)
		{
			if(layoutManager is LinearLayoutManager linearLayoutManager)
			{
				var isFirstItem = linearLayoutManager.FindFirstCompletelyVisibleItemPosition() == 0;
				var isLastItem = linearLayoutManager.FindLastCompletelyVisibleItemPosition() == layoutManager.ItemCount - 1;

				if (isFirstItem || isLastItem)
					return null;
			}

			return base.FindSnapView(layoutManager);
		}
	}
}
