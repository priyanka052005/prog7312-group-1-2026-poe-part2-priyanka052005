namespace SmartX_Part1.Models;

public class DeploymentNode
{
    public string Name { get; set; } = "";
    public string NodeType { get; set; } = "";
    public bool IsConfigured { get; set; }
    public DeploymentNode? Child { get; set; }

    public static DeploymentNode CreateDemoHierarchy()
    {
        return new DeploymentNode
        {
            Name = "Facility A",
            NodeType = "Facility",
            IsConfigured = true,
            Child = new DeploymentNode
            {
                Name = "Zone 1",
                NodeType = "Zone",
                IsConfigured = true,
                Child = new DeploymentNode
                {
                    Name = "Sub-Zone B",
                    NodeType = "Sub-Zone",
                    IsConfigured = true,
                    Child = new DeploymentNode
                    {
                        Name = "ESP32-A1",
                        NodeType = "Sensor",
                        IsConfigured = true
                    }
                }
            }
        };
    }
}
