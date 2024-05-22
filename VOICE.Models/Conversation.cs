using VOICE.Models;

public class Conversation
    {
        public List<Message> History { get; private set; }

        public Conversation()
        {
            History = new List<Message>();
        }

        public void AddUserMessage(string message)
        {
            History.Add(new Message { role = nameof(Roles.user), content = message });
        }

        public void AddBotMessage(string message)
        {
            History.Add(new Message { role = nameof(Roles.assistant), content = message });
        }

        public void AddSystemMessage(string message)
        {
            History.Add(new Message { role = nameof(Roles.system), content = message });
        }

        public void ClearHistory()
        {
            History.Clear();
        }

        public List<Message> GetHistory()
        {
            return History;
        }
    }