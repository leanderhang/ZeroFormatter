﻿using System.Collections.Generic;
using Xunit;
using ZeroFormatter.Formatters;
using ZeroFormatter.Internal;
using ZeroFormatter.Segments;

namespace ZeroFormatter.DotNetCore.Tests
{
    public class ListSegmentTest
    {
        FixedListSegment<DefaultResolver, int> CreateFixedList(DirtyTracker tracker)
        {
            byte[] bytes = null;
            BinaryUtil.WriteInt32(ref bytes, 0, 6); // length 4...
            BinaryUtil.WriteInt32(ref bytes, 4 * 1, 99);
            BinaryUtil.WriteInt32(ref bytes, 4 * 2, 55);
            BinaryUtil.WriteInt32(ref bytes, 4 * 3, 3);
            BinaryUtil.WriteInt32(ref bytes, 4 * 4, 9423423);
            BinaryUtil.WriteInt32(ref bytes, 4 * 5, -432423);
            BinaryUtil.WriteInt32(ref bytes, 4 * 6, 2423);

            int _;
            var list = FixedListSegment<DefaultResolver, int>.Create(tracker, bytes, 0, out _);
            return list;
        }

        VariableListSegment<DefaultResolver, string> CraeteVariableList(DirtyTracker tracker)
        {
            var bytes = ZeroFormatterSerializer.Serialize<IList<string>>(new[] { "hoge", "あああ", "huga", null, "takotako", "chop", "!^ :a<>" });

            int _;
            var list = VariableListSegment<DefaultResolver, string>.Create(tracker, bytes, 0, out _);
            return list;
        }

        [Fact]
        public void FixedListSegment()
        {
            var tracker = new DirtyTracker();

            var list = CreateFixedList(tracker);

            // no cache
            {
                list.Count.Is(6);

                list[0].Is(99);
                list[1].Is(55);
                list[2].Is(3);
                list[3].Is(9423423);
                list[4].Is(-432423);
                list[5].Is(2423);

                list.Contains(3).IsTrue();
                list.Contains(-1).IsFalse();
                list.IndexOf(3).Is(2);
                list.IndexOf(9999).Is(-1);

                var copy = new int[6];
                list.CopyTo(copy, 0);
                copy.Is(99, 55, 3, 9423423, -432423, 2423);

                copy = new int[15];
                list.CopyTo(copy, 4);
                copy.Is(0, 0, 0, 0, 99, 55, 3, 9423423, -432423, 2423, 0, 0, 0, 0, 0);

                list[2] = 999999;
                list[2].Is(999999);

                using (var e = list.GetEnumerator())
                {
                    var l = new List<int>();
                    while (e.MoveNext())
                    {
                        l.Add(e.Current);
                    }
                    l.Is(99, 55, 999999, 9423423, -432423, 2423);
                }
            }

            // cache on
            {
                list = CreateFixedList(tracker);
                list.Add(-4141);
                list.Add(21);
                list.Add(11);
                list.Is(99, 55, 3, 9423423, -432423, 2423, -4141, 21, 11);

                list = CreateFixedList(tracker);
                list.Clear();
                list.Count.Is(0);
                list.Add(999);
                list.Is(999);

                list = CreateFixedList(tracker);
                list.Insert(3, -5);
                list.Is(99, 55, 3, -5, 9423423, -432423, 2423);
                list.Insert(0, 100);
                list.Is(100, 99, 55, 3, -5, 9423423, -432423, 2423);

                list = CreateFixedList(tracker);
                list.Remove(9423423).IsTrue();
                list.Remove(-1).IsFalse();
                list.Is(99, 55, 3, -432423, 2423);

                list = CreateFixedList(tracker);
                list.RemoveAt(3);
                list.Is(99, 55, 3, -432423, 2423);
                list.RemoveAt(0);
                list.Is(55, 3, -432423, 2423);
                list.RemoveAt(3);
                list.Is(55, 3, -432423);
            }
        }

        [Fact]
        public void VariableListSegment()
        {
            var tracker = new DirtyTracker();

            // "hoge", "あああ", "huga", "takotako", "chop", "かきくけこ", "!^ :a<>"
            var list = CraeteVariableList(tracker);

            // no cache
            {
                list.Count.Is(7);

                list[0].Is("hoge");
                list[1].Is("あああ");
                list[2].Is("huga");
                list[3].Is((string)null);
                list[4].Is("takotako");
                list[5].Is("chop");
                list[6].Is("!^ :a<>");

                // cached contents
                list[0].Is("hoge");
                list[1].Is("あああ");
                list[2].Is("huga");
                list[3].Is((string)null);
                list[4].Is("takotako");
                list[5].Is("chop");
                list[6].Is("!^ :a<>");

                list.Contains("あああ").IsTrue();
                list.Contains("?").IsFalse();
                list.IndexOf("あああ").Is(1);
                list.IndexOf("!?!?!?!?").Is(-1);

                var copy = new string[7];
                list.CopyTo(copy, 0);
                copy.Is("hoge", "あああ", "huga", null, "takotako", "chop", "!^ :a<>");

                copy = new string[15];
                list.CopyTo(copy, 4);
                copy.Is(null, null, null, null, "hoge", "あああ", "huga", null, "takotako", "chop", "!^ :a<>", null, null, null, null);

                list[2] = "takoyaki x";
                list[2].Is("takoyaki x");

                using (var e = list.GetEnumerator())
                {
                    var l = new List<string>();
                    while (e.MoveNext())
                    {
                        l.Add(e.Current);
                    }
                    l.Is("hoge", "あああ", "takoyaki x", null, "takotako", "chop", "!^ :a<>");
                }
            }

            // cache on
            {
                list = CraeteVariableList(tracker);
                list.Add("0");
                list.Add(null);
                list.Add("2");
                list.Is("hoge", "あああ", "huga", null, "takotako", "chop", "!^ :a<>", "0", null, "2");

                list = CraeteVariableList(tracker);
                list.Clear();
                list.Count.Is(0);
                list.Add("c");
                list.Is("c");

                list = CraeteVariableList(tracker);
                list.Insert(3, "-5");
                list.Is("hoge", "あああ", "huga", "-5", null, "takotako", "chop", "!^ :a<>");
                list.Insert(0, "zero");
                list.Is("zero", "hoge", "あああ", "huga", "-5", null, "takotako", "chop", "!^ :a<>");

                list = CraeteVariableList(tracker);
                list.Remove("あああ").IsTrue();
                list.Remove("-1").IsFalse();
                list.Is("hoge", "huga", null, "takotako", "chop", "!^ :a<>");

                list = CraeteVariableList(tracker);
                list.RemoveAt(3);
                list.Is("hoge", "あああ", "huga", "takotako", "chop", "!^ :a<>");
                list.RemoveAt(0);
                list.Is("あああ", "huga", "takotako", "chop", "!^ :a<>");
                list.RemoveAt(4);
                list.Is("あああ", "huga", "takotako", "chop");
            }
        }
    }
}
