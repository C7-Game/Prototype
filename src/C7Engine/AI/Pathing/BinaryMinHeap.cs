using System;
using System.Collections.Generic;

namespace C7Engine.Pathing {
	/**
	 * https://en.wikipedia.org/wiki/Binary_heap
	 */
	public class BinaryMinHeap<TValue> where TValue : IComparable<TValue> {
		private readonly List<TValue> data = new List<TValue>();

		public int count { get => data.Count; }

		// average O(1), worst case O(log N)
		public void insert(TValue v) {
			data.Add(v);
			siftUp(data.Count - 1);
		}

		// extract the smallest value, O(log N)
		public TValue extract() {
			TValue result = data[0];
			data[0] = data[data.Count - 1];
			data.RemoveAt(data.Count - 1);
			if (data.Count > 0) {
				siftDown(0);
			}
			return result;
		}

		private void siftUp(int childIndex) {
			if (childIndex == 0) return;
			int parentIndex = getParentIndex(childIndex);
			if (!isRightOrder(parentIndex, childIndex)) {
				swap(parentIndex, childIndex);
				siftUp(parentIndex);
			}
		}

		private void siftDown(int parentIndex) {
			int leftChild = getLeftChild(parentIndex);
			int rightChild = leftChild + 1;
			if (rightChild < data.Count) {
				// two children
				bool leftShouldBeHigher = isRightOrder(leftChild, rightChild);
				int topChildIndex = leftShouldBeHigher ? leftChild : rightChild;
				if (!isRightOrder(parentIndex, topChildIndex)) {
					swap(parentIndex, topChildIndex);
					siftDown(topChildIndex);
				}
				return;
			}

			if (leftChild >= data.Count) {
				// no children
				return;
			}

			// one children
			if (!isRightOrder(parentIndex, leftChild)) {
				swap(parentIndex, leftChild);
			}
		}

		private bool isRightOrder(int parentIndex, int childIndex) {
			return data[parentIndex].CompareTo(data[childIndex]) <= 0;
		}

		private void swap(int index, int otherIndex) {
			(data[index], data[otherIndex]) = (data[otherIndex], data[index]);
		}

		// 0 -> 0;  1,2 -> 0;  3,4 -> 1; 5,6 -> 2;
		private static int getParentIndex(int index) {
			return (index + 1) / 2 - 1;
		}

		// 0 -> 1; 1 -> 3; 2 -> 5; 3 -> 7
		private static int getLeftChild(int index) {
			return (index + 1) * 2 - 1;
		}
	}
}
