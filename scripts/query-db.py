#!/usr/bin/env python3
"""Query Notification Aggregator database"""

import sqlite3
import os
from datetime import datetime

db_path = os.path.expandvars(r"%APPDATA%\NotificationAggregator\notifications.db")

if not os.path.exists(db_path):
    print(f"❌ Database not found: {db_path}")
    exit(1)

conn = sqlite3.connect(db_path)
cursor = conn.cursor()

# Total count
cursor.execute("SELECT COUNT(*) FROM Notifications")
total = cursor.fetchone()[0]

print("\n📊 Notification Aggregator Stats")
print("=" * 50)
print(f"Total Notifications: {total}\n")

# By severity
print("By Severity:")
cursor.execute("""
SELECT 
  CASE Severity 
    WHEN 0 THEN 'Info' 
    WHEN 1 THEN 'Warning' 
    WHEN 2 THEN 'Error' 
    WHEN 3 THEN 'Critical' 
  END as Severity,
  COUNT(*) as Count
FROM Notifications
GROUP BY Severity
ORDER BY Severity DESC
""")
for severity, count in cursor.fetchall():
    print(f"  {severity:10} : {count}")

# By source
print("\nBy Source:")
cursor.execute("SELECT Source, COUNT(*) as Count FROM Notifications GROUP BY Source")
for source, count in cursor.fetchall():
    print(f"  {source:20} : {count}")

# Recent
print("\nRecent Notifications (last 5):")
cursor.execute("""
SELECT 
  CASE Severity WHEN 0 THEN 'ℹ️ ' WHEN 1 THEN '⚠️ ' WHEN 2 THEN '❌' WHEN 3 THEN '🔴' END as Icon,
  Title,
  strftime('%Y-%m-%d %H:%M:%S', OccurredAt) as Time
FROM Notifications 
ORDER BY OccurredAt DESC 
LIMIT 5
""")
for icon, title, time in cursor.fetchall():
    print(f"  {icon} [{time}] {title}")

conn.close()
print("\n✓ Service is collecting notifications!\n")
