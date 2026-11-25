namespace Padel.Application.Common.Notifications;

/// <summary>A message to a member, e.g. an email.</summary>
public sealed record Notification(string To, string Subject, string Body);
