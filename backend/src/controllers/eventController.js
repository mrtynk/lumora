const pool = require("../db/pool");

const MAX_EVENT_LIMIT = 100;
const DEFAULT_SAFE_LIMIT = 100;

const parseEventLimit = (rawLimit) => {
  if (rawLimit === undefined) {
    return null;
  }

  const parsedLimit = Number(rawLimit);

  if (!Number.isInteger(parsedLimit) || parsedLimit <= 0) {
    return DEFAULT_SAFE_LIMIT;
  }

  return Math.min(parsedLimit, MAX_EVENT_LIMIT);
};

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
    const filterColumns = {
      childId: "child_id",
      eventType: "event_type",
      region: "region",
      puzzleType: "puzzle_type",
    };
    const conditions = [];
    const queryValues = [];

    for (const [queryName, columnName] of Object.entries(filterColumns)) {
      const rawValue = req.query[queryName];

      if (typeof rawValue === "string" && rawValue.trim() !== "") {
        queryValues.push(rawValue.trim());
        conditions.push(`${columnName} = $${queryValues.length}`);
      }
    }

    let query = "SELECT * FROM game_events";

    if (conditions.length > 0) {
      query += ` WHERE ${conditions.join(" AND ")}`;
    }

    query += " ORDER BY created_at DESC";

    const limit = parseEventLimit(req.query.limit);
    if (limit !== null) {
      queryValues.push(limit);
      query += ` LIMIT $${queryValues.length}`;
    }

    const result = await pool.query(query, queryValues);

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
