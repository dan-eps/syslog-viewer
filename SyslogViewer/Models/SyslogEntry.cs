using System;

namespace SyslogViewer.Models;

public class SyslogEntry
{
	public DateTime Time { get; set; }
	public string Host { get; set; }
	public string Message { get; set; }
}