const currency = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD'
});
const dateTime = new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' })

export const formatMoney = (value: number) => currency.format(value)

// Backend stores UTC (DateTime.UtcNow) but EF reads it back without a "Z",
// which the browser would treat as local time. Mark zone-less values as UTC.
const hasZone = /(Z|[+-]\d{2}:\d{2})$/
export const parseApiDate = (iso: string) => new Date(hasZone.test(iso) ? iso : `${iso}Z`)

export const formatDate = (iso: string) => dateTime.format(parseApiDate(iso))
