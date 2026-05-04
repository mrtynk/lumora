const pool = require("../db/pool");

function clampScore(score) {
  if (score < 0) return 0;
  if (score > 100) return 100;
  return Math.round(score);
}

function countEvents(events, eventType) {
  return events.filter((event) => event.event_type === eventType).length;
}

const getScoresByChildId = async (req, res) => {
  try {
    const { childId } = req.params;

    const result = await pool.query(
      `SELECT * FROM game_events 
       WHERE child_id = $1 
       ORDER BY created_at ASC`,
      [childId]
    );

    const events = result.rows;

    if (events.length === 0) {
      return res.status(404).json({
        message: "Bu childId için kayıtlı event bulunamadı.",
        childId,
      });
    }

    const puzzleStarted = countEvents(events, "puzzle_started");
    const puzzleSolved = countEvents(events, "puzzle_solved");
    const hintRequested = countEvents(events, "hint_requested");
    const retryAttempt = countEvents(events, "retry_attempt");
    const areaExplored = countEvents(events, "area_explored");
    const npcHelped = countEvents(events, "npc_helped");
    const choiceMade = countEvents(events, "choice_made");
    const wrongClick = countEvents(events, "wrong_click");

    const successRate =
      puzzleStarted === 0 ? 0 : (puzzleSolved / puzzleStarted) * 100;

    const independenceScore =
      puzzleSolved === 0
        ? 50
        : 100 - (hintRequested / puzzleSolved) * 25;

    const persistenceScore = 50 + retryAttempt * 15;

    const curiosityScore = 40 + areaExplored * 20;

    const socialScore = 40 + npcHelped * 20;

    const strategyScore = 50 + choiceMade * 15 + puzzleSolved * 5;

    const attentionScore = successRate - wrongClick * 10;

    const scores = {
      attention: clampScore(attentionScore),
      persistence: clampScore(persistenceScore),
      curiosity: clampScore(curiosityScore),
      independence: clampScore(independenceScore),
      strategy: clampScore(strategyScore),
      social: clampScore(socialScore),
    };

    res.json({
      message: "Çocuk davranış skorları başarıyla hesaplandı.",
      childId,
      totalEvents: events.length,
      rawMetrics: {
        puzzleStarted,
        puzzleSolved,
        hintRequested,
        retryAttempt,
        areaExplored,
        npcHelped,
        choiceMade,
        wrongClick,
      },
      scores,
      note: "Bu skorlar klinik değerlendirme değildir. Oyun içi davranışlardan üretilen kural tabanlı ön gözlem skorlarıdır.",
    });
  } catch (error) {
    console.error("Skor hesaplama hatası:", error);

    res.status(500).json({
      message: "Skorlar hesaplanırken hata oluştu.",
      error: error.message,
    });
  }
};

module.exports = {
  getScoresByChildId,
};