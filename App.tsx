import React, { useEffect, useState } from "react";
import { StatusBar } from "expo-status-bar";
import {
  SafeAreaView, View, Text, TextInput, TouchableOpacity,
  FlatList, ActivityIndicator, StyleSheet, Platform,
  StatusBar as RNStatusBar
} from "react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { api } from "./api";

type Sentiment = { label?: string; score?: number | null };
type ApiItem = {
  id: number; alias?: string; userAlias?: string; text: string;
  sentiment?: Sentiment; sentimentLabel?: string; sentimentScore?: number | null;
  createdAt: string;
};
type Message = { id: number; alias: string; text: string; sentiment: Sentiment; createdAt: string; };

const normalize = (m: ApiItem): Message => ({
  id: m.id,
  alias: m.alias ?? m.userAlias ?? "",
  text: m.text,
  sentiment: m.sentiment ?? { label: m.sentimentLabel, score: m.sentimentScore },
  createdAt: m.createdAt,
});

export default function App() {
  const [alias, setAlias] = useState("oguz");
  const [aliasReady, setAliasReady] = useState(false);
  const [text, setText] = useState("");
  const [items, setItems] = useState<Message[]>([]);
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState("");
  const [refreshing, setRefreshing] = useState(false);

  // 1) alias'ı yükle (hazır olunca işaretle)
  useEffect(() => {
    (async () => {
      try {
        const saved = await AsyncStorage.getItem("alias");
        if (saved) setAlias(saved);
      } finally {
        setAliasReady(true);
      }
    })();
  }, []);

  // 2) alias değişince kaydet
  useEffect(() => {
    if (aliasReady) AsyncStorage.setItem("alias", alias).catch(() => {});
  }, [alias, aliasReady]);

  // 3) listeyi alias hazır olunca çek (tek yer)
  useEffect(() => {
    if (aliasReady) fetchList();
  }, [aliasReady, alias]);

  async function fetchList() {
    try {
      setErr("");
      const { data } = await api.get<ApiItem[]>("/api/messages", {
        params: { alias, limit: 50 },
        validateStatus: s => s < 500,
      });
      if (typeof data === "string") { setErr("API JSON dönmüyor."); return; }
      const rows = Array.isArray(data) ? data : [data];
      setItems(rows.map(normalize));
    } catch (e) {
      console.error(e);
      setErr("Liste alınamadı.");
    }
  }

  async function onRefresh() {
    setRefreshing(true);
    await fetchList();
    setRefreshing(false);
  }

  async function send() {
    if (!text.trim()) return;
    try {
      setLoading(true); setErr("");
      const { data } = await api.post<ApiItem>("/api/messages", { alias, text });
      setItems(prev => [normalize(data), ...prev]);
      setText("");
    } catch (e) {
      console.error(e);
      setErr("Gönderilemedi.");
    } finally {
      setLoading(false);
    }
  }

  function sentimentColor(label?: string) {
    if (label === "positive") return "#1e8e3e";
    if (label === "negative") return "#c41c1c";
    return "#555";
  }

  const renderItem = ({ item }: { item: Message }) => (
    <View style={s.card}>
      <View style={s.row}>
        <Text style={s.alias}>@{item.alias}</Text>
        <Text style={[s.sentiment, { color: sentimentColor(item.sentiment.label) }]}>
          {item.sentiment.label ?? "-"}{" "}
          {item.sentiment.score != null ? `(${Number(item.sentiment.score).toFixed(2)})` : ""}
        </Text>
      </View>
      <Text>{item.text}</Text>
      <Text style={s.time}>{new Date(item.createdAt).toLocaleString()}</Text>
    </View>
  );

  const topPad = (Platform.OS === "android" ? (RNStatusBar.currentHeight ?? 0) : 0) + 12;

  return (
    <SafeAreaView style={[s.wrap, { paddingTop: topPad }]}>
      <StatusBar style="auto" />
      <Text style={s.title}>Chat + Duygu Analizi</Text>

      <View style={s.inputRow}>
        <TextInput
          style={[s.input, { width: 120 }]}
          placeholder="rumuz"
          value={alias}
          onChangeText={setAlias}
        />
        <TextInput
          style={[s.input, { flex: 1 }]}
          placeholder="mesaj"
          value={text}
          onChangeText={setText}
          onSubmitEditing={send}
          returnKeyType="send"
        />
        <TouchableOpacity
          style={s.button}
          onPress={send}
          disabled={loading || !text.trim()}
        >
          <Text>{loading ? "..." : "Gönder"}</Text>
        </TouchableOpacity>
      </View>

      {!!err && <Text style={s.error}>{err}</Text>}

      <FlatList
        data={items}
        keyExtractor={m => m.id.toString()}
        renderItem={renderItem}
        refreshing={refreshing}
        onRefresh={onRefresh}
        contentContainerStyle={{ paddingBottom: 40 }}
        ListEmptyComponent={<Text style={s.empty}>Henüz mesaj yok. Bir şey yazıp Gönder’e bas.</Text>}
      />

      {loading && <ActivityIndicator style={{ marginTop: 8 }} />}
    </SafeAreaView>
  );
}

const s = StyleSheet.create({
  wrap: { flex: 1, paddingHorizontal: 16, backgroundColor: "#fff" },
  title: { fontSize: 20, fontWeight: "700", marginBottom: 12, textAlign: "center" }, // ← ortalı
  input: { borderWidth: 1, borderColor: "#ddd", borderRadius: 10, paddingHorizontal: 10, height: 40 },
  button: { borderWidth: 1, borderColor: "#ddd", borderRadius: 10, paddingHorizontal: 12, height: 40, alignItems: "center", justifyContent: "center", marginLeft: 8 },
  inputRow: { flexDirection: "row", alignItems: "center", gap: 8, marginBottom: 10 },
  card: { borderWidth: 1, borderColor: "#eee", borderRadius: 12, padding: 10, marginBottom: 10 },
  row: { flexDirection: "row", justifyContent: "space-between", marginBottom: 6 },
  alias: { fontWeight: "700" },
  sentiment: { fontWeight: "600" },
  time: { marginTop: 6, color: "#666", fontSize: 12 },
  error: { color: "#c41c1c", marginBottom: 8 },
  empty: { color: "#666", marginTop: 8 },
});
