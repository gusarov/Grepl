using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Grepl.Model
{
	/// <summary>
	/// Contains a color-printable message
	/// </summary>
	class ColoredMessage
	{
		public readonly List<MessagePart> Parts = new List<MessagePart>();

		public static bool operator ==(ColoredMessage a, ColoredMessage b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(ColoredMessage a, ColoredMessage b)
		{
			return !a.Equals(b);
		}

		public override int GetHashCode()
		{
			int hash = 0;
			foreach (var item in Parts)
			{
				hash = HashCode.Combine(hash, item);
			}
			return hash;
		}

		public override bool Equals(object obj)
		{
			if (obj is ColoredMessage cm)
			{
				if (Parts.Count != cm.Parts.Count)
				{
					return false;
				}

				for (int i = 0; i < Parts.Count; i++)
				{
					var a = Parts[i];
					var b = cm.Parts[i];
					if (!a.Equals(b))
					{
						return false;
					}
				}

				return true;
			}
			return false;
		}
	}

	abstract class MessagePart
	{
		public abstract void Accept(IMessagePartVisitor visitor);
	}

	class SetColorMessagePart : MessagePart
	{
		public SetColorMessagePart(ConsoleColor color)
		{
			Color = color;
		}

		public ConsoleColor Color { get; }

		#region Equality

		protected bool Equals(SetColorMessagePart other)
		{
			return Color == other.Color;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((SetColorMessagePart) obj);
		}

		public override int GetHashCode()
		{
			return (int) Color;
		}

		#endregion

		public override void Accept(IMessagePartVisitor visitor) => visitor.Visit(this);

	}

	class ResetColorMessagePart : MessagePart
	{
		#region Equality

		protected bool Equals(ResetColorMessagePart other)
		{
			return true;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((ResetColorMessagePart) obj);
		}

		public override int GetHashCode()
		{
			return 1;
		}

		#endregion

		public override void Accept(IMessagePartVisitor visitor) => visitor.Visit(this);

	}

	class WriteMessagePart : MessagePart
	{
		public WriteMessagePart(string message)
		{
			Text = message;
		}

		public string Text { get; }

		#region Equality

		protected bool Equals(WriteMessagePart other)
		{
			return Text == other.Text;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((WriteMessagePart) obj);
		}

		public override int GetHashCode()
		{
			return (Text != null ? Text.GetHashCode() : 0);
		}

		#endregion

		public override void Accept(IMessagePartVisitor visitor) => visitor.Visit(this);

	}

	class WriteLineMessagePart : WriteMessagePart
	{
		public WriteLineMessagePart(string line)
			: base(line + Environment.NewLine)
		{

		}
	}

	/*
	class ReWriteProgressMessagePart : MessagePart
	{

	}
	*/
}
