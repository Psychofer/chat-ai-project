import { useState, useEffect } from 'react';
import axios from 'axios';
import './App.css';

// Backend API URL'inizi buraya koyun
const API_URL = 'https://chat-backend-f8ky.onrender.com/api';

function App() {
  const [username, setUsername] = useState('');
  const [userId, setUserId] = useState(null);
  const [messages, setMessages] = useState([]);
  const [newMessage, setNewMessage] = useState('');
  const [loading, setLoading] = useState(false);

  // Sayfa yüklendiğinde mesajları getir
  useEffect(() => {
    if (userId) {
      loadMessages();
      // Her 3 saniyede bir mesajları güncelle
      const interval = setInterval(loadMessages, 3000);
      return () => clearInterval(interval);
    }
  }, [userId]);

  // Tüm mesajları yükle
  const loadMessages = async () => {
    try {
      const response = await axios.get(`${API_URL}/messages`);
      setMessages(response.data);
    } catch (error) {
      console.error('Mesajlar yüklenemedi:', error);
    }
  };

  // Kullanıcı kaydı
  const handleRegister = async (e) => {
    e.preventDefault();
    if (!username.trim()) return;

    try {
      setLoading(true);
      const response = await axios.post(`${API_URL}/users/register`, {
        username: username.trim()
      });
      setUserId(response.data.userId);
      setUsername(response.data.username);
      alert(`Hoş geldin, ${response.data.username}! 🎉`);
    } catch (error) {
      alert(error.response?.data?.message || 'Kayıt başarısız!');
    } finally {
      setLoading(false);
    }
  };

  // Mesaj gönder
  const handleSendMessage = async (e) => {
    e.preventDefault();
    if (!newMessage.trim() || !userId) return;

    try {
      setLoading(true);
      const response = await axios.post(`${API_URL}/messages/send`, {
        userId: userId,
        text: newMessage.trim()
      });
      
      setMessages([...messages, response.data]);
      setNewMessage('');
    } catch {
      alert('Mesaj gönderilemedi!');
    } finally {
      setLoading(false);
    }
  };

  // Duygu emoji'si
  const getSentimentEmoji = (sentiment) => {
    switch (sentiment?.toLowerCase()) {
      case 'pozitif': return '😊';
      case 'negatif': return '😢';
      default: return '😐';
    }
  };

  // Duygu rengi
  const getSentimentColor = (sentiment) => {
    switch (sentiment?.toLowerCase()) {
      case 'pozitif': return '#4ade80';
      case 'negatif': return '#f87171';
      default: return '#94a3b8';
    }
  };

  // Giriş yapmadıysa kayıt ekranı göster
  if (!userId) {
    return (
      <div className="app">
        <div className="login-container">
          <h1>💬 AI Chat</h1>
          <p>Duygu analizi ile mesajlaş!</p>
          <form onSubmit={handleRegister}>
            <input
              type="text"
              placeholder="Kullanıcı adın..."
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              disabled={loading}
            />
            <button type="submit" disabled={loading}>
              {loading ? 'Giriş yapılıyor...' : 'Giriş Yap'}
            </button>
          </form>
        </div>
      </div>
    );
  }

  // Chat ekranı
  return (
    <div className="app">
      <div className="chat-container">
        <div className="chat-header">
          <h2>💬 AI Chat</h2>
          <span className="username">👤 {username}</span>
        </div>

        <div className="messages-container">
          {messages.length === 0 ? (
            <p className="empty-state">Henüz mesaj yok. İlk mesajı sen at! 🚀</p>
          ) : (
            messages.map((msg) => (
              <div
                key={msg.messageId}
                className={`message ${msg.userId === userId ? 'own' : 'other'}`}
              >
                <div className="message-header">
                  <strong>{msg.username}</strong>
                  <span className="sentiment" style={{ color: getSentimentColor(msg.sentiment) }}>
                    {getSentimentEmoji(msg.sentiment)} {msg.sentiment} ({Math.round(msg.sentimentScore * 100)}%)
                  </span>
                </div>
                <div className="message-text">{msg.text}</div>
                <div className="message-time">
                  {new Date(msg.createdAt).toLocaleTimeString('tr-TR')}
                </div>
              </div>
            ))
          )}
        </div>

        <form className="message-form" onSubmit={handleSendMessage}>
          <input
            type="text"
            placeholder="Mesajını yaz..."
            value={newMessage}
            onChange={(e) => setNewMessage(e.target.value)}
            disabled={loading}
          />
          <button type="submit" disabled={loading || !newMessage.trim()}>
            {loading ? '⏳' : '📤'}
          </button>
        </form>
      </div>
    </div>
  );
}

export default App;