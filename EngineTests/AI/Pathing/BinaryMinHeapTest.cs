using System;
using System.Collections.Generic;
using C7Engine.Pathing;
using Xunit;

namespace EngineTests {
	public class BinaryMinHeapTest {
		private void checkBunchInsert<T>(List<T> list) where T:IComparable<T> {
			BinaryMinHeap<T> heap = new BinaryMinHeap<T>();
			foreach (T v in list) {
				heap.insert(v);
			}

			Assert.Equal(list.Count, heap.count);

			List<T> result = new List<T>();
			while (heap.count > 0) {
				result.Add(heap.extract());
			}
			Assert.Equal(list.Count, result.Count);

			List<T> sortedCopy = new List<T>(list);
			sortedCopy.Sort();
			Assert.Equal(result, sortedCopy);
		}

		public List<int> generateLargeList() {
			List<int> lst = new List<int>();
			for (int i = 0; i < 1000; ++i) {
				lst.Add(i);
			}

			Random random = new Random(0x1337);
			for (int i = lst.Count - 1; i > 1 ; --i) {
				var pos = random.Next(i - 1);
				(lst[i], lst[pos]) = (lst[pos], lst[i]);
			}

			return lst;
		}

		[Fact]
		public void testBinaryMinHeap() {
			List<int> singleValue = new List<int>(){1};
			List<int> fewValues = new List<int>() {9, 1, 3, 5, 6};
			List<int> fewRepeatingValues = new List<int>() {1, 5, 6, 1, 5, 6};
			List<float> floatValues = new List<float>() {float.MinValue, 0.1f, 0, 1, -1, 1000};

			checkBunchInsert(singleValue);
			checkBunchInsert(fewValues);
			checkBunchInsert(fewRepeatingValues);
			checkBunchInsert(floatValues);
			checkBunchInsert(generateLargeList());
		}
	}
}
