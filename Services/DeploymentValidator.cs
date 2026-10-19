using SmartX_Part1.Models;

namespace SmartX_Part1.Services;

public static class DeploymentValidator
{
    // Required recursion.
    // Base case: no child -> current node is the final node.
    public static bool ValidatePath(DeploymentNode node)
    {
        if (node is null)
            return false;

        if (!node.IsConfigured || string.IsNullOrWhiteSpace(node.Name))
            return false;

        if (node.Child is null)
            return true;

        return ValidatePath(node.Child);
    }
}
