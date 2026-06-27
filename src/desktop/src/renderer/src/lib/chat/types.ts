export type ChatRole = 'user' | 'assistant'

export type ChatMessage = {
  id: string
  role: ChatRole
  content: string
  createdAt: Date
}

export type ChatThread = {
  id: string
  title: string
  preview: string
  updatedAt: Date
  messages: ChatMessage[]
}
