import gradio as gr
from transformers import pipeline

# Duygu analizi modeli yükleniyor (Hugging Face'ten)
# Bu model Türkçe metinleri analiz edebilir
sentiment_analyzer = pipeline(
    "sentiment-analysis",
    model="savasy/bert-base-turkish-sentiment-cased"
)

def analyze_sentiment(text):
    """
    Gelen metni analiz eder ve duygu skorunu döner
    
    Args:
        text (str): Analiz edilecek mesaj
        
    Returns:
        dict: Duygu etiketi ve skoru
    """
    if not text or len(text.strip()) == 0:
        return {"label": "NEUTRAL", "score": 0.0}
    
    try:
        result = sentiment_analyzer(text)[0]
        
        # Sonucu formatla
        sentiment_label = result['label'].upper()
        sentiment_score = round(result['score'], 2)
        
        # Türkçe etiketlere çevir
        label_map = {
            "POSITIVE": "pozitif",
            "NEGATIVE": "negatif",
            "NEUTRAL": "nötr"
        }
        
        turkish_label = label_map.get(sentiment_label, "nötr")
        
        return {
            "sentiment": turkish_label,
            "score": sentiment_score,
            "confidence": sentiment_score
        }
    except Exception as e:
        return {
            "sentiment": "nötr",
            "score": 0.5,
            "confidence": 0.5,
            "error": str(e)
        }

# Gradio API arayüzü oluştur
demo = gr.Interface(
    fn=analyze_sentiment,
    inputs=gr.Textbox(label="Mesaj", placeholder="Bir mesaj yazın..."),
    outputs=gr.JSON(label="Duygu Analizi Sonucu"),
    title="💭 Türkçe Duygu Analizi",
    description="Mesajınızı yazın, duygusunu analiz edelim!",
    examples=[
        ["Bugün harika bir gün!"],
        ["Çok üzgünüm ve mutsuzum."],
        ["Merhaba, nasılsın?"]
    ]
)

if __name__ == "__main__":
    demo.launch()