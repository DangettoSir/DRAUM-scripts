using System.Collections.Generic;

public class AgentGoal {
    public string Name { get; }
    public float BasePriority { get; private set; }
    public float Priority { get; set; }
    public HashSet<AgentBelief> DesiredEffects { get; } = new();
    
    AgentGoal(string name) {
        Name = name;
        Priority = BasePriority;
    }
    
    public class Builder {
        readonly AgentGoal goal;

        public Builder(string name) {
            goal = new AgentGoal(name);
        }
        
        public Builder WithPriority(float priority) {
            goal.BasePriority = priority;
            goal.Priority = priority;
            return this;
        }

        public Builder WithDesiredEffect(AgentBelief effect) {
            goal.DesiredEffects.Add(effect);
            return this;
        }

        public AgentGoal Build() {
            return goal;
        }
    }
}