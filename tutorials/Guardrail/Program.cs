using PocketFlow;

// --- Build nodes ---
var userInputNode = new UserInputNode();
var guardrailNode = new GuardrailNode();
var llmNode       = new LlmNode();

// --- Wire the flow ---
userInputNode.On("validate").Then(guardrailNode);
guardrailNode.On("retry").Then(userInputNode);   // Invalid input → re-prompt
guardrailNode.On("process").Then(llmNode);
llmNode.On("continue").Then(userInputNode);      // Loop back after reply

// --- Run ---
var flow = new Flow(start: userInputNode);
flow.Run(new Dictionary<string, object>());
