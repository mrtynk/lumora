import { useEffect, useState } from "react";
import axios from "axios";
import "./App.css";

const API_BASE_URL = "http://localhost:5000";

const scoreLabels = {
  attention: "Dikkat",
  persistence: "Azim",
  curiosity: "Merak",
  independence: "Bağımsızlık",
  strategy: "Stratejik Düşünme",
  social: "Sosyal Eğilim",
};

const scoreDescriptions = {
  attention: "Bulmaca başarısı ve hata oranına göre hesaplanır.",
  persistence: "Tekrar deneme davranışını gösterir.",
  curiosity: "Keşfedilen alanlara göre hesaplanır.",
  independence: "İpucu kullanmadan çözme eğilimini gösterir.",
  strategy: "Seçim ve çözüm davranışlarına göre hesaplanır.",
  social: "NPC yardım ve etkileşim davranışlarını gösterir.",
};

function App() {
  const [childId, setChildId] = useState("demo-child-001");
  const [scoresData, setScoresData] = useState(null);
  const [eventsData, setEventsData] = useState([]);
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  const fetchDashboard = async () => {
    try {
      setLoading(true);
      setErrorMessage("");

      const scoresResponse = await axios.get(
        `${API_BASE_URL}/api/scores/${childId}`
      );

      const eventsResponse = await axios.get(`${API_BASE_URL}/api/events`);

      setScoresData(scoresResponse.data);
      setEventsData(eventsResponse.data.events || []);
    } catch (error) {
      console.error(error);
      setErrorMessage(
        "Veriler alınamadı. Backend çalışıyor mu ve childId doğru mu kontrol et."
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDashboard();
  }, []);

  const scores = scoresData?.scores || {};
  const rawMetrics = scoresData?.rawMetrics || {};

  return (
    <div className="page">
      <header className="hero">
        <div>
          <p className="eyebrow">Lumora Parent Panel</p>
          <h1>Çocuk Davranış Analitiği Paneli</h1>
          <p className="hero-text">
            Oyun içi davranış eventleri backend üzerinden işlenir ve ebeveynler
            için anlaşılır beceri skorlarına dönüştürülür.
          </p>
        </div>

        <div className="child-card">
          <span>Aktif Çocuk ID</span>
          <strong>{childId}</strong>
        </div>
      </header>

      <section className="toolbar">
        <input
          value={childId}
          onChange={(event) => setChildId(event.target.value)}
          placeholder="childId gir"
        />
        <button onClick={fetchDashboard} disabled={loading}>
          {loading ? "Yükleniyor..." : "Verileri Yenile"}
        </button>
      </section>

      {errorMessage && <div className="error">{errorMessage}</div>}

      <section className="summary-grid">
        <div className="summary-card">
          <span>Toplam Event</span>
          <strong>{scoresData?.totalEvents ?? "-"}</strong>
        </div>
        <div className="summary-card">
          <span>Çözülen Puzzle</span>
          <strong>{rawMetrics.puzzleSolved ?? "-"}</strong>
        </div>
        <div className="summary-card">
          <span>İpucu Kullanımı</span>
          <strong>{rawMetrics.hintRequested ?? "-"}</strong>
        </div>
        <div className="summary-card">
          <span>Keşif Eventi</span>
          <strong>{rawMetrics.areaExplored ?? "-"}</strong>
        </div>
      </section>

      <section>
        <div className="section-title">
          <h2>6 Beceri Skoru</h2>
          <p>Kural tabanlı ön gözlem skorları</p>
        </div>

        <div className="score-grid">
          {Object.entries(scoreLabels).map(([key, label]) => (
            <div className="score-card" key={key}>
              <div className="score-top">
                <h3>{label}</h3>
                <strong>{scores[key] ?? 0}</strong>
              </div>

              <div className="progress">
                <div
                  className="progress-fill"
                  style={{ width: `${scores[key] ?? 0}%` }}
                />
              </div>

              <p>{scoreDescriptions[key]}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="panel-grid">
        <div className="info-panel">
          <h2>AI Destekli Öneri Alanı</h2>
          <p>
            Bu bölüm ileride skorlar üzerinden otomatik öneriler üretecek.
            Örneğin dikkat skoru düşükse kısa süreli görsel tarama oyunları,
            azim skoru düşükse daha düşük baskılı puzzle görevleri önerilebilir.
          </p>
          <div className="recommendation">
            {scores.attention < 60
              ? "Dikkat skoru için kısa ve ödüllü bulmaca görevleri önerilir."
              : "Genel performans dengeli görünüyor. Mevcut oyun akışı sürdürülebilir."}
          </div>
        </div>

        <div className="info-panel">
          <h2>Akademik Not</h2>
          <p>
            Bu panel klinik tanı aracı değildir. Oyun içi davranışlardan elde
            edilen event verilerini kural tabanlı bir ön gözlem sistemine
            dönüştüren prototip bir ebeveyn analiz arayüzüdür.
          </p>
        </div>
      </section>

      <section>
        <div className="section-title">
          <h2>Son Oyun Eventleri</h2>
          <p>Backend veritabanından gelen ham davranış kayıtları</p>
        </div>

        <div className="event-list">
          {eventsData.slice(0, 10).map((event) => (
            <div className="event-row" key={event.id}>
              <div>
                <strong>{event.event_type}</strong>
                <span>
                  {event.region || "region yok"} ·{" "}
                  {event.puzzle_type || "puzzle yok"}
                </span>
              </div>
              <p>{event.value || "Açıklama yok"}</p>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}

export default App;