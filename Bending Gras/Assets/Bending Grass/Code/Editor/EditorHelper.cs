using System;
using System.Text;
using UnityEngine;

namespace BendinGrass
{
	/// <summary>
	/// Contains some function used mostly in the editor. 
	/// </summary>
	public static class EditorHelper
	{
		public const float k = 1000f;
		public const float M = k * k;

		public static bool HasFlag(int value, int flag)
		{
			return (value & flag) == flag;
		}

		public static Rect RectDeform(Rect r, float top = 0f, float left = 0f, float bottom = 0f, float right = 0f)
		{
			r.y -= top;
			r.height += top;

			r.x -= left;
			r.width += left;

			r.height += bottom;
			r.width += right;
			return (r);
		}

		public static string ColorTex(string text, string colore)
		{
			StringBuilder builder = new StringBuilder(text.Length + 25);
			builder.Append("<color=\"#").Append(colore).Append("\">").Append(text).Append("</color>");
			return (builder.ToString());
		}

		public static Rect[] HorizontalSplit(Rect parent, float space, params float[] percents)
		{
			Rect[] rects = new Rect[percents.Length + 1];

			int i = 0;
			float lastx = parent.x;
			for (i = 0; i < rects.Length; i++)
			{
				Rect r;
				bool first = i == 0;
				bool last = i >= rects.Length - 1;
				if (!last)
					r = new Rect(lastx, parent.y, parent.width * percents[i], parent.height);
				else
					r = new Rect(lastx, parent.y, parent.width - (lastx - parent.x), parent.height);
				lastx = r.xMax;

				r = EditorHelper.RectDeform(r, 0, first ? 0 : (-space / 2), 0, last ? 0 : (-space / 2));
				rects[i] = r;
			}
			return rects;
		}

		public static Rect[] HorizontalSplit(Rect parent, float space, int num)
		{
			num = Mathf.Max(1, num);
			float[] perc = new float[num - 1];
			for (int i = 0; i < perc.Length; i++)
			{
				perc[i] = 1f / (float)num;
			}
			return HorizontalSplit(parent, space, perc);
		}

		public static Rect[] HorizontalSplitSize(Rect parent, float space, params float[] sizes)
		{

			float[] perc = new float[sizes.Length];
			for (int i = 0; i < perc.Length; i++)
			{
				perc[i] = sizes[i] / parent.width;
			}
			return HorizontalSplit(parent, space, perc);
		}

		public static Rect[] VerticalSplit(Rect parent, float space, params float[] percents)
		{
			Rect[] rects = new Rect[percents.Length + 1];

			int i = 0;
			float lasty = parent.y;
			for (i = 0; i < rects.Length; i++)
			{
				Rect r;
				bool first = i == 0;
				bool last = i >= rects.Length - 1;
				if (!last)
					r = new Rect(parent.x, lasty, parent.width, parent.height * percents[i]);
				else
					r = new Rect(parent.x, lasty, parent.width, parent.height - (lasty - parent.y));
				lasty = r.yMax;

				r = EditorHelper.RectDeform(r, first ? 0 : (-space / 2), 0, last ? 0 : (-space / 2), 0);
				rects[i] = r;
			}
			return rects;
		}


		public static Rect[] VerticalSplit(Rect parent, float space, int num)
		{
			num = Mathf.Max(1, num);
			float[] perc = new float[num - 1];
			for (int i = 0; i < perc.Length; i++)
			{
				perc[i] = 1f / (float)num;
			}
			return VerticalSplit(parent, space, perc);
		}

		public static Rect[] VerticalSplitSize(Rect parent, float space, params float[] sizes)
		{

			float[] perc = new float[sizes.Length];
			for (int i = 0; i < perc.Length; i++)
			{
				perc[i] = sizes[i] / parent.height;
			}
			return VerticalSplit(parent, space, perc);
		}

		public static bool IsMouseOver(Rect r)
		{
			return r.Contains(Event.current.mousePosition);

		}

	}

	public static class FloatExtension
	{

		public static string ToShortString(this float f)
		{
			string value = "";
			if (f < EditorHelper.k)
			{
				if (f < 100f)
					value = f.ToString("F1");
				else
					value = f.ToString("F0");
			}
			else if (f < EditorHelper.M)
			{
				f = f / EditorHelper.k;
				if (f < 100f)
					value = f.ToString("F1");
				else
					value = f.ToString("F0");
				value += "k";
			}
			else if (f >= EditorHelper.M)
			{
				f = f / EditorHelper.M;
				if (f < 100f)
					value = f.ToString("F1");
				else
					value = f.ToString("F0");
				value += "M";
			}
			return value;
		}
	}

	public static class StringExtension
	{
		public static string ColorText(this string s, Color c)
		{
			return EditorHelper.ColorTex(s, ColorUtility.ToHtmlStringRGBA(c));
		}
	}

	[Flags]
	public enum Anchor
	{
		NONE = 0,
		TOP = 1,
		RIGHT = 2,
		BOTTOM = 4,
		LEFT = 8,

	}


}
