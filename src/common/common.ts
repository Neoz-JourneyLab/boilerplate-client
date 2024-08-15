export function readCSVFile(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => {
      resolve(reader.result as string)
    }
    reader.onerror = () => {
      reject(reader.error)
    }
    reader.readAsText(file)
  })
}

function getDaysInMonth(year: number, month: number): number {
  // Le mois suivant avec le jour 0 renvoie le dernier jour du mois précédent
  return new Date(year, month + 1, 0).getDate()
}

export function getDaysInMonthFromDate(date: Date): number {
  const year = date.getFullYear()
  const month = date.getMonth()
  return getDaysInMonth(year, month)
}

export const GetBackCol = function (col: string, prefer_white: boolean = false) {
  const c = col.replace('#', '')
  const r = parseInt(c.substring(0, 2), 16) * 0.299
  const g = parseInt(c.substring(2, 4), 16) * 0.587
  const b = parseInt(c.substring(4, 6), 16) * 0.144
  if (r + g + b > (prefer_white ? 200 : 100)) return '#000'
  else return '#FFF'
}