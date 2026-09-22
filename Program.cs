var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); // <-- Register controllers
builder.Services.AddHttpClient<RestaurantApiService>();
builder.Services.AddSingleton<OrderTakingAI>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Serve a simple chat interface at the root URL
app.MapGet("/", () => Results.Content(GetChatHtml(), "text/html"));

// Map controller endpoints (e.g., POST /api/chat)
app.MapControllers();

app.Run();

// Helper to return inline HTML for the chat interface
static string GetChatHtml()
{
    return @"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, viewport-fit=cover'>
    <meta name='theme-color' content='#4A90D9'>
    <title>Restaurant Order AI</title>
    <style>
        :root {
            --primary: #4A90D9;
            --primary-dark: #357ABD;
            --secondary: #F5F5F5;
            --user-bubble: #DCF8C6;
            --ai-bubble: #FFFFFF;
            --text-dark: #333;
            --text-light: #666;
            --border: #E0E0E0;
            --shadow: 0 2px 10px rgba(0,0,0,0.1);
            --radius: 18px;
            --font: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
        }

        * { box-sizing: border-box; margin: 0; padding: 0; }

        html, body {
            height: 100%;
            /* Prevent bounce/overscroll on mobile */
            overscroll-behavior: none;
        }

        body {
            font-family: var(--font);
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            min-height: 100dvh; /* Dynamic viewport height — fixes mobile browser address bar */
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 20px;
            -webkit-tap-highlight-color: transparent;
        }

        .chat-container {
            background: #fff;
            width: 100%;
            max-width: 700px;
            height: 90vh;
            height: 90dvh;
            max-height: 800px;
            border-radius: 20px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            display: flex;
            flex-direction: column;
            overflow: hidden;
        }

        .chat-header {
            background: var(--primary);
            color: white;
            padding: 20px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            flex-shrink: 0;
            gap: 10px;
        }

        .chat-header h1 {
            font-size: 1.5rem;
            font-weight: 600;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }

        .clear-btn {
            background: rgba(255,255,255,0.2);
            border: none;
            color: white;
            padding: 8px 16px;
            border-radius: 20px;
            cursor: pointer;
            font-size: 0.9rem;
            transition: background 0.3s;
            flex-shrink: 0;
            -webkit-tap-highlight-color: transparent;
            /* Larger touch target on mobile */
            min-height: 40px;
        }

        .clear-btn:hover,
        .clear-btn:active {
            background: rgba(255,255,255,0.35);
        }

        .chat-box {
            flex: 1;
            padding: 20px;
            overflow-y: auto;
            background: var(--secondary);
            display: flex;
            flex-direction: column;
            gap: 15px;
            -webkit-overflow-scrolling: touch; /* Smooth momentum scrolling on iOS */
            scroll-behavior: smooth;
        }

        .message {
            max-width: 80%;
            padding: 12px 18px;
            border-radius: var(--radius);
            line-height: 1.4;
            word-wrap: break-word;
            overflow-wrap: break-word;
            animation: fadeIn 0.3s ease;
            white-space: pre-wrap;
        }

        @keyframes fadeIn {
            from { opacity: 0; transform: translateY(10px); }
            to { opacity: 1; transform: translateY(0); }
        }

        .user-msg {
            background: var(--user-bubble);
            align-self: flex-end;
            border-bottom-right-radius: 4px;
            color: var(--text-dark);
        }

        .ai-msg {
            background: var(--ai-bubble);
            align-self: flex-start;
            border-bottom-left-radius: 4px;
            box-shadow: 0 1px 2px rgba(0,0,0,0.1);
            color: var(--text-dark);
        }

        .typing-indicator {
            display: inline-block;
            align-self: flex-start;
            background: var(--ai-bubble);
            padding: 12px 18px;
            border-radius: var(--radius);
            border-bottom-left-radius: 4px;
            box-shadow: 0 1px 2px rgba(0,0,0,0.1);
        }

        .typing-indicator span {
            display: inline-block;
            width: 8px;
            height: 8px;
            border-radius: 50%;
            background: #999;
            margin: 0 2px;
            animation: typing 1.4s infinite both;
        }

        .typing-indicator span:nth-child(2) { animation-delay: 0.2s; }
        .typing-indicator span:nth-child(3) { animation-delay: 0.4s; }

        @keyframes typing {
            0% { transform: translateY(0); }
            50% { transform: translateY(-5px); }
            100% { transform: translateY(0); }
        }

        .input-area {
            display: flex;
            padding: 15px;
            background: #fff;
            border-top: 1px solid var(--border);
            flex-shrink: 0;
            gap: 8px;
            /* Respect iPhone home-bar safe area */
            padding-bottom: max(15px, env(safe-area-inset-bottom));
        }

        .input-area input {
            flex: 1;
            padding: 12px 18px;
            border: 2px solid var(--border);
            border-radius: 25px;
            font-size: 16px; /* 16px prevents auto-zoom on iOS when focused */
            outline: none;
            transition: border-color 0.3s;
            font-family: inherit;
            min-width: 0; /* Prevents overflow in flexbox */
            -webkit-appearance: none;
            appearance: none;
        }

        .input-area input:focus {
            border-color: var(--primary);
        }

        .input-area button {
            padding: 12px 25px;
            background: var(--primary);
            color: white;
            border: none;
            border-radius: 25px;
            font-size: 1rem;
            cursor: pointer;
            transition: background 0.3s;
            flex-shrink: 0;
            -webkit-tap-highlight-color: transparent;
            min-height: 44px; /* Apple's recommended min touch target */
            font-family: inherit;
        }

        .input-area button:hover {
            background: var(--primary-dark);
        }

        .input-area button:active {
            background: var(--primary-dark);
            transform: scale(0.97);
        }

        .input-area button:disabled {
            background: #ccc;
            cursor: not-allowed;
        }

        /* Responsive adjustments — mobile */
        @media (max-width: 600px) {
            body {
                padding: 0;
                align-items: stretch;
            }

            .chat-container {
                /* Full screen edge-to-edge on mobile */
                height: 100vh;
                height: 100dvh;
                max-height: none;
                border-radius: 0;
                box-shadow: none;
                max-width: 100%;
            }

            .chat-header {
                padding: 14px 16px;
                /* Push header content below the notch */
                padding-top: max(14px, env(safe-area-inset-top));
            }

            .chat-header h1 {
                font-size: 1.05rem;
            }

            .clear-btn {
                padding: 8px 14px;
                font-size: 0.85rem;
            }

            .chat-box {
                padding: 14px 12px;
                gap: 10px;
            }

            .message {
                max-width: 88%;
                padding: 10px 14px;
                font-size: 0.95rem;
                border-radius: 16px;
            }

            .input-area {
                padding: 10px 12px;
                padding-bottom: max(10px, env(safe-area-inset-bottom));
                gap: 6px;
            }

            .input-area input {
                padding: 12px 16px;
                border-radius: 22px;
            }

            .input-area button {
                padding: 12px 18px;
                border-radius: 22px;
            }
        }

        /* Extra-small phones */
        @media (max-width: 360px) {
            .chat-header h1 { font-size: 0.95rem; }
            .message { max-width: 92%; font-size: 0.9rem; }
            .input-area button { padding: 12px 14px; font-size: 0.9rem; }
        }
    </style>
</head>
<body>
    <div class='chat-container'>
        <div class='chat-header'>
            <h1>🍽️ Restaurant Order AI</h1>
            <button class='clear-btn' onclick='clearConversation()'>Clear</button>
        </div>
        <div class='chat-box' id='chat-box'></div>
        <div class='input-area'>
            <input type='text' id='message-input' placeholder='Type your message...' />
            <button id='send-btn' onclick='sendMessage()'>Send</button>
        </div>
    </div>

    <script>
        let sessionId = localStorage.getItem('chatSessionId');
        if (!sessionId) {
            sessionId = 'session-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
            localStorage.setItem('chatSessionId', sessionId);
        }

        const chatBox = document.getElementById('chat-box');
        const input = document.getElementById('message-input');
        const sendBtn = document.getElementById('send-btn');

        function addMessage(text, sender) {
            const msgDiv = document.createElement('div');
            msgDiv.textContent = text;
            msgDiv.className = 'message ' + (sender === 'user' ? 'user-msg' : 'ai-msg');
            chatBox.appendChild(msgDiv);
            chatBox.scrollTop = chatBox.scrollHeight;
        }

        function showTypingIndicator() {
            const typingDiv = document.createElement('div');
            typingDiv.className = 'typing-indicator';
            typingDiv.id = 'typing-indicator';
            typingDiv.innerHTML = '<span></span><span></span><span></span>';
            chatBox.appendChild(typingDiv);
            chatBox.scrollTop = chatBox.scrollHeight;
        }

        function hideTypingIndicator() {
            const typing = document.getElementById('typing-indicator');
            if (typing) typing.remove();
        }

        async function sendMessage() {
            const message = input.value.trim();
            if (!message) return;
            addMessage('You: ' + message, 'user');
            input.value = '';
            sendBtn.disabled = true;
            showTypingIndicator();

            try {
                const response = await fetch('/api/chat', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ sessionId: sessionId, message: message })
                });
                const data = await response.json();
                hideTypingIndicator();
                if (data && data.response) {
                    addMessage('AI: ' + data.response, 'ai');
                } else {
                    addMessage('AI: (no response)', 'ai');
                }
            } catch (error) {
                hideTypingIndicator();
                addMessage('AI: Error - ' + error.message, 'ai');
            } finally {
                sendBtn.disabled = false;
                input.focus();
            }
        }

        function clearConversation() {
            // Generate new session ID and clear chat display
            sessionId = 'session-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
            localStorage.setItem('chatSessionId', sessionId);
            chatBox.innerHTML = '';
            // Send a greeting to start new conversation
            sendInitialGreeting();
        }

        async function sendInitialGreeting() {
            showTypingIndicator();
            try {
                const response = await fetch('/api/chat', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ sessionId: sessionId, message: '' }) // empty message triggers greeting
                });
                const data = await response.json();
                hideTypingIndicator();
                if (data && data.response) {
                    addMessage('AI: ' + data.response, 'ai');
                }
            } catch (e) {
                hideTypingIndicator();
            }
        }

        // Allow Enter key to send message
        input.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') sendMessage();
        });

        // Keep input visible when mobile keyboard opens (visualViewport API)
        if (window.visualViewport) {
            window.visualViewport.addEventListener('resize', function() {
                chatBox.scrollTop = chatBox.scrollHeight;
            });
        }

        // Initial greeting from AI
        window.onload = sendInitialGreeting;
    </script>
</body>
</html>";
}