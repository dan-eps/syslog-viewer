using System;

namespace SyslogViewer.Models;

public class SyslogEntry
{
	public string Host { get; set; }
	public int? Facility { get; set; }
	public int? Severity { get; set; }
	public DateTime? Time { get; set; }
	public string? Hostname { get; set; }
	public string? AppName { get; set; }
	public int? ProcessId { get; set; }
	public int? MessageId { get; set; }
	public string? StructuredData { get; set; }
	public string? Message { get; set; }
}