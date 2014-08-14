using System;

namespace WebSocketSharp
{
	public interface IWSLogger
	{
		void Trace(string message);
		void Info(string message);
		void Debug(string message);
		void Warn(string message);
		void Error(string message);
		void Fatal(string message);
	}
}

