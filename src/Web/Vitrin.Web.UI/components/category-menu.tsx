'use client';

import { useEffect } from 'react';
import { useProductStore } from '@/core/application/useProductStore';
import { cn } from '@/lib/utils';

export function CategoryMenu() {
  const { topics, selectedTopicSlug, fetchTopics, setTopicFilter } = useProductStore();

  useEffect(() => {
    fetchTopics();
  }, [fetchTopics]);

  if (!topics || topics.length === 0) return null;

  return (
    <div className="w-full overflow-x-auto pb-4 mb-6 -mx-4 px-4 sm:mx-0 sm:px-0 hide-scrollbar flex items-center gap-2">
      <button
        onClick={() => setTopicFilter(null)}
        className={cn(
          "whitespace-nowrap px-4 py-2 rounded-full text-sm font-semibold transition-all border",
          selectedTopicSlug === null
            ? "bg-[#00A170] text-white border-[#00A170] shadow-md"
            : "bg-card text-foreground border-border hover:border-[#00A170]/50 hover:bg-[#00A170]/10"
        )}
      >
        Tümü
      </button>
      
      {topics.map((topic) => (
        <button
          key={topic.id}
          onClick={() => setTopicFilter(topic.slug)}
          className={cn(
            "whitespace-nowrap px-4 py-2 rounded-full text-sm font-semibold transition-all border",
            selectedTopicSlug === topic.slug
              ? "bg-[#00A170] text-white border-[#00A170] shadow-md"
              : "bg-card text-foreground border-border hover:border-[#00A170]/50 hover:bg-[#00A170]/10"
          )}
        >
          {topic.name}
        </button>
      ))}
    </div>
  );
}
