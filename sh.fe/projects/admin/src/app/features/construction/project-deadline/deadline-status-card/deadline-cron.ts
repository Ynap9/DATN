const DAILY_CRON = /^(\d{1,2}) (\d{1,2}) \* \* \*$/;

// "m h * * *" -> "Hằng ngày HH:mm"; cron khác hiện nguyên chuỗi
export function formatDeadlineCron(cron: string) {
    const match = DAILY_CRON.exec(cron.trim());
    if (!match) return cron;
    const [, minute, hour] = match;
    return `Hằng ngày ${hour.padStart(2, '0')}:${minute.padStart(2, '0')}`;
}
