const pool = require("../db/pool");

const createEvent = async (req, res) => {
  try {
    const { childId, eventType, region, puzzleType, value } = req.body;

    if (!childId || !eventType) {
      return res.status(400).json({
        message: "childId ve eventType zorunludur.",
      });
    }

    const result = await pool.query(
      `INSERT INTO game_events 
       (child_id, event_type, region, puzzle_type, value)
       VALUES ($1, $2, $3, $4, $5)
       RETURNING *`,
      [
        childId,
        eventType,
        region || null,
        puzzleType || null,
        value || null,
      ]
    );

    res.status(201).json({
      message: "Oyun eventi başarıyla kaydedildi.",
      event: result.rows[0],
    });
  } catch (error) {
    console.error("Event kaydetme hatası:", error);

    res.status(500).json({
      message: "Event kaydedilirken hata oluştu.",
      error: error.message,
    });
  }
};

const getEvents = async (req, res) => {
  try {
    const result = await pool.query(
      `SELECT * FROM game_events ORDER BY created_at DESC`
    );

    res.json({
      message: "Eventler başarıyla listelendi.",
      count: result.rows.length,
      events: result.rows,
    });
  } catch (error) {
    console.error("Event listeleme hatası:", error);

    res.status(500).json({
      message: "Eventler listelenirken hata oluştu.",
      error: error.message,
    });
  }
};

module.exports = {
  createEvent,
  getEvents,
};