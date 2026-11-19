using Tenere.Core.Models;

namespace Tenere.Core.Abstractions;

public enum PrivacyDecision
{
    Allow,
    Suspect,
    Block
}

public interface IPrivacyFilter
{
    PrivacyDecision Evaluate(ClipboardSnapshot snapshot);
}