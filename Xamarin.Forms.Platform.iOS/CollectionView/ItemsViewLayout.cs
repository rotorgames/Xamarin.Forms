using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CoreGraphics;
using Foundation;
using UIKit;
using System.Linq;

namespace Xamarin.Forms.Platform.iOS
{
	public abstract class ItemsViewLayout : UICollectionViewFlowLayout, IUICollectionViewDelegateFlowLayout
	{
		readonly ItemsLayout _itemsLayout;
		bool _determiningCellSize;
		bool _disposed;

		protected ItemsViewLayout(ItemsLayout itemsLayout)
		{
			Xamarin.Forms.CollectionView.VerifyCollectionViewFlagEnabled(nameof(ItemsViewLayout));

			_itemsLayout = itemsLayout;
			_itemsLayout.PropertyChanged += LayoutOnPropertyChanged;

			var scrollDirection = itemsLayout.Orientation == ItemsLayoutOrientation.Horizontal
				? UICollectionViewScrollDirection.Horizontal
				: UICollectionViewScrollDirection.Vertical;

			Initialize(scrollDirection);
		}

		protected override void Dispose(bool disposing)
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			if (disposing)
			{
				if (_itemsLayout != null)
				{
					_itemsLayout.PropertyChanged += LayoutOnPropertyChanged;
				}
			}

			base.Dispose(disposing);
		}

		void LayoutOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChanged)
		{
			HandlePropertyChanged(propertyChanged);
		}

		protected virtual void HandlePropertyChanged(PropertyChangedEventArgs  propertyChanged)
		{
			// Nothing to do here for now; may need something here when we implement Snapping
		}

		public nfloat ConstrainedDimension { get; set; }

		public Func<UICollectionViewCell> GetPrototype { get; set; }

		// TODO hartez 2018/09/14 17:24:22 Long term, this needs to use the ItemSizingStrategy enum and not be locked into bool	
		public bool UniformSize { get; set; }

		public abstract void ConstrainTo(CGSize size);

		[Export("scrollViewDidEndDecelerating:")]
		public void DecelerationEnded(UIScrollView scrollView)
		{
			ScrollToSnapElement();
		}

		[Export("scrollViewDidEndDragging:willDecelerate:")]
		public void DraggingEnded(UIScrollView scrollView, bool willDecelerate)
		{
			ScrollToSnapElement();
		}

		void ScrollToSnapElement()
		{
			if (_itemsLayout.SnapPointsType != SnapPointsType.Mandatory && _itemsLayout.SnapPointsType != SnapPointsType.MandatorySingle)
				return;

			var contentOffset = CollectionView.ContentOffset;
			var contentSize = CollectionView.ContentSize;

			var targetRect = new CGRect(
				contentOffset.X,
				contentOffset.Y,
				CollectionView.Bounds.Size.Width,
				CollectionView.Bounds.Size.Height);

			var lineStartPosition = _itemsLayout.Orientation == ItemsLayoutOrientation.Vertical ? targetRect.GetMinY() : targetRect.GetMinX();
			var lineEndPosition = _itemsLayout.Orientation == ItemsLayoutOrientation.Vertical ? targetRect.GetMaxY() : targetRect.GetMaxX();
			var lineContentSize = _itemsLayout.Orientation == ItemsLayoutOrientation.Vertical ? contentSize.Height : contentSize.Width;

			if (lineStartPosition <= 0 || lineEndPosition >= lineContentSize)
				return;

			var layoutAttributes = LayoutAttributesForElementsInRect(targetRect);

			if (!layoutAttributes.Any())
				return;

			UICollectionViewLayoutAttributes targetLayoutAttributes = null;
			UICollectionViewScrollPosition scrollPosition;

			if (_itemsLayout.SnapPointsAlignment == SnapPointsAlignment.Start)
			{
				scrollPosition = _itemsLayout.Orientation == ItemsLayoutOrientation.Vertical 
					? UICollectionViewScrollPosition.Top 
					: UICollectionViewScrollPosition.Left;

				targetLayoutAttributes = layoutAttributes.First();

				if (!IsHalfVisible(_itemsLayout.Orientation, targetLayoutAttributes.Frame, targetRect) && layoutAttributes.Count() > 1)
					targetLayoutAttributes = layoutAttributes[1];
			}
			else if (_itemsLayout.SnapPointsAlignment == SnapPointsAlignment.End)
			{
				scrollPosition = _itemsLayout.Orientation == ItemsLayoutOrientation.Vertical
					? UICollectionViewScrollPosition.Bottom
					: UICollectionViewScrollPosition.Right;

				targetLayoutAttributes = layoutAttributes.Last();

				if (!IsHalfVisible(_itemsLayout.Orientation, targetLayoutAttributes.Frame, targetRect) && layoutAttributes.Count() > 1)
					targetLayoutAttributes = layoutAttributes[layoutAttributes.Count() - 2];
			}
			else
			{
				scrollPosition = _itemsLayout.Orientation == ItemsLayoutOrientation.Vertical
					? UICollectionViewScrollPosition.CenteredVertically
					: UICollectionViewScrollPosition.CenteredHorizontally;

				var targetCenterPosition = _itemsLayout.Orientation == ItemsLayoutOrientation.Vertical ? targetRect.GetMidY() : targetRect.GetMidX();
				var minDistance = double.PositiveInfinity;

				foreach (var item in layoutAttributes)
				{
					var itemCenterPosition = _itemsLayout.Orientation == ItemsLayoutOrientation.Vertical ? item.Center.Y : item.Center.X;

					var itemMinDistance = Math.Abs(targetCenterPosition - itemCenterPosition);

					if (itemMinDistance < minDistance)
					{
						targetLayoutAttributes = item;
						minDistance = itemMinDistance;
					}
				}
			}

			if (targetLayoutAttributes == null)
				return;

			CollectionView.ScrollToItem(targetLayoutAttributes.IndexPath, scrollPosition, true);
		}

		bool IsHalfVisible(ItemsLayoutOrientation orientation, CGRect itemRect, CGRect containerRect)
		{
			var itemCenterPosition = orientation == ItemsLayoutOrientation.Vertical ? itemRect.GetMidY() : itemRect.GetMidX(); ;
			var minContainerSize = orientation == ItemsLayoutOrientation.Vertical ? containerRect.GetMinY() : containerRect.GetMinX();
			var maxContainerSize = orientation == ItemsLayoutOrientation.Vertical ? containerRect.GetMaxY() : containerRect.GetMaxX();

			return itemCenterPosition >= minContainerSize && itemCenterPosition <= maxContainerSize;
		}

		[Export("collectionView:layout:insetForSectionAtIndex:")]
		[CompilerGenerated]
		public virtual UIEdgeInsets GetInsetForSection(UICollectionView collectionView, UICollectionViewLayout layout,
			nint section)
		{
			return UIEdgeInsets.Zero;
		}

		[Export("collectionView:layout:minimumInteritemSpacingForSectionAtIndex:")]
		[CompilerGenerated]
		public virtual nfloat GetMinimumInteritemSpacingForSection(UICollectionView collectionView,
			UICollectionViewLayout layout, nint section)
		{
			return (nfloat)0.0;
		}

		[Export("collectionView:layout:minimumLineSpacingForSectionAtIndex:")]
		[CompilerGenerated]
		public virtual nfloat GetMinimumLineSpacingForSection(UICollectionView collectionView,
			UICollectionViewLayout layout, nint section)
		{
			return (nfloat)0.0;
		}

		public void PrepareCellForLayout(ItemsViewCell cell)
		{
			if (_determiningCellSize)
			{
				return;
			}

			if (EstimatedItemSize == CGSize.Empty)
			{
				cell.ConstrainTo(ItemSize);
			}
			else
			{
				cell.ConstrainTo(ConstrainedDimension);
			}
		}

		public override bool ShouldInvalidateLayoutForBoundsChange(CGRect newBounds)
		{
			var shouldInvalidate = base.ShouldInvalidateLayoutForBoundsChange(newBounds);

			if (shouldInvalidate)
			{
				UpdateConstraints(newBounds.Size);
			}

			return shouldInvalidate;
		}

		protected void DetermineCellSize()
		{
			if (GetPrototype == null)
			{
				return;
			}

			_determiningCellSize = true;

			if (!Forms.IsiOS10OrNewer)
			{
				// iOS 9 will throw an exception during auto layout if no EstimatedSize is set
				EstimatedItemSize = new CGSize(1, 1);
			}

			if (!(GetPrototype() is ItemsViewCell prototype))
			{
				return;
			}

			prototype.ConstrainTo(ConstrainedDimension);

			var measure = prototype.Measure();

			if (UniformSize)
			{
				ItemSize = measure;

				// Make sure autolayout is disabled 
				EstimatedItemSize = CGSize.Empty;
			}
			else
			{
				EstimatedItemSize = measure;
			}

			_determiningCellSize = false;
		}

		bool ConstraintsMatchScrollDirection(CGSize size)
		{
			if (ScrollDirection == UICollectionViewScrollDirection.Vertical)
			{
				return ConstrainedDimension == size.Width;
			}

			return ConstrainedDimension == size.Height;
		}

		void Initialize(UICollectionViewScrollDirection scrollDirection)
		{
			ScrollDirection = scrollDirection;
		}

		void UpdateCellConstraints()
		{
			var cells = CollectionView.VisibleCells;

			for (int n = 0; n < cells.Length; n++)
			{
				if (cells[n] is ItemsViewCell constrainedCell)
				{
					PrepareCellForLayout(constrainedCell);
				}
			}
		}

		void UpdateConstraints(CGSize size)
		{
			if (ConstraintsMatchScrollDirection(size))
			{
				return;
			}

			ConstrainTo(size);
			UpdateCellConstraints();
		}
	}
}